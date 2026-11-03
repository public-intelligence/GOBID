using GOBID.Application.DTOs.Policies;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace GOBID.Infrastructure.Services;

public class PolicyEvaluatorService : IPolicyEvaluatorService
{
    private readonly DbContext _dbContext;

    public PolicyEvaluatorService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response<PolicyEvaluationResultDto>> EvaluateAsync(
        Guid userApplicationId, string ipAddress, string? userAgent)
    {
        try
        {
            // Cargar userApplication para obtener userId y organizationId
            var userApp = await _dbContext.Set<UserApplication>()
                .Include(ua => ua.OrganizationApplication)
                .FirstOrDefaultAsync(ua => ua.UserApplicationId == userApplicationId && ua.IsActive);

            if (userApp == null)
                return new Response<PolicyEvaluationResultDto>("UserApplication no encontrado o inactivo", false);

            // Obtener todas las políticas activas asignadas al userApplication
            var assignments = await _dbContext.Set<UserApplicationPolicy>()
                .Include(x => x.Policy)
                    .ThenInclude(p => p.PolicyType)
                .Include(x => x.Policy)
                    .ThenInclude(p => p.PolicyConditions)
                .Where(x => x.UserApplicationId == userApplicationId
                         && x.IsActive
                         && x.Policy.IsActive)
                .ToListAsync();

            // Sin políticas asignadas → acceso permitido por defecto
            if (!assignments.Any())
            {
                return new Response<PolicyEvaluationResultDto>(new PolicyEvaluationResultDto
                {
                    IsAllowed = true,
                    Result = "ALLOWED",
                    Reason = "Sin políticas asignadas"
                });
            }

            var utcNow = DateTime.UtcNow;
            var denyReasons = new List<string>();
            var allowPolicies = assignments.Where(a => a.Policy.Effect == "ALLOW").ToList();
            var denyPolicies = assignments.Where(a => a.Policy.Effect == "DENY").ToList();

            // Evaluar políticas DENY primero (si alguna aplica → denegar)
            foreach (var assignment in denyPolicies)
            {
                var (matches, reason) = EvaluatePolicy(assignment.Policy, ipAddress, utcNow);
                if (matches)
                {
                    var denyReason = !string.IsNullOrWhiteSpace(reason)
                        ? reason
                        : BuildDenyReason(assignment.Policy, ipAddress, utcNow);
                    await SaveLogAsync(userApp, assignment.Policy, "DENIED", denyReason, ipAddress, userAgent);
                    return new Response<PolicyEvaluationResultDto>(new PolicyEvaluationResultDto
                    {
                        IsAllowed = false,
                        Result = "DENIED",
                        Reason = denyReason
                    });
                }
            }

            // Evaluar políticas ALLOW (al menos una debe cumplirse)
            if (allowPolicies.Any())
            {
                foreach (var assignment in allowPolicies)
                {
                    var (matches, reason) = EvaluatePolicy(assignment.Policy, ipAddress, utcNow);
                    if (matches)
                    {
                        await SaveLogAsync(userApp, assignment.Policy, "ALLOWED", null, ipAddress, userAgent);
                        return new Response<PolicyEvaluationResultDto>(new PolicyEvaluationResultDto
                        {
                            IsAllowed = true,
                            Result = "ALLOWED"
                        });
                    }
                    // Solo agregar razones significativas (reason vacío = política no aplica en este contexto)
                    if (!string.IsNullOrWhiteSpace(reason))
                        denyReasons.Add(reason);
                }

                // Ninguna política ALLOW se cumplió
                var finalReason = denyReasons.Count > 0
                    ? string.Join(" | ", denyReasons)
                    : "Acceso denegado por politicas de la Organización";
                await SaveLogAsync(userApp, allowPolicies.First().Policy, "DENIED", finalReason, ipAddress, userAgent);
                return new Response<PolicyEvaluationResultDto>(new PolicyEvaluationResultDto
                {
                    IsAllowed = false,
                    Result = "DENIED",
                    Reason = finalReason
                });
            }

            // Solo había políticas DENY y ninguna aplica → permitido
            await SaveLogAsync(userApp, denyPolicies.First().Policy, "ALLOWED", null, ipAddress, userAgent);
            return new Response<PolicyEvaluationResultDto>(new PolicyEvaluationResultDto
            {
                IsAllowed = true,
                Result = "ALLOWED"
            });
        }
        catch (Exception ex)
        {
            return new Response<PolicyEvaluationResultDto>($"Error al evaluar políticas: {ex.Message}", false);
        }
    }

    // ─── Dispatcher por tipo de política ─────────────────────────────────────

    private (bool matches, string reason) EvaluatePolicy(Policy policy, string ipAddress, DateTime utcNow)
    {
        return policy.PolicyType.Name switch
        {
            "IP_RANGE" => EvaluateIpRange(policy, ipAddress),
            "TIME_WINDOW" => EvaluateTimeWindow(policy, utcNow),
            _ => (false, $"Tipo de política '{policy.PolicyType.Name}' no soportado")
        };
    }

    /// <summary>
    /// Genera un mensaje descriptivo para cuando una política DENY hace match (reason viene vacío del evaluador).
    /// </summary>
    private static string BuildDenyReason(Policy policy, string ipAddress, DateTime utcNow)
    {
        var conditions = policy.PolicyConditions.ToDictionary(c => c.ConditionKey, c => c.ConditionValue);

        return policy.PolicyType.Name switch
        {
            "IP_RANGE" => conditions.TryGetValue("IpRangeStart", out var s) && conditions.TryGetValue("IpRangeEnd", out var e)
                ? $"Política '{policy.Name}': IP {ipAddress} está dentro del rango denegado [{s} - {e}]"
                : $"Política '{policy.Name}': acceso denegado por rango de IP",

            "TIME_WINDOW" => conditions.TryGetValue("StartTime", out var st) && conditions.TryGetValue("EndTime", out var et)
                ? $"Política '{policy.Name}': acceso denegado en horario [{st} - {et}]"
                : $"Política '{policy.Name}': acceso denegado por ventana de tiempo",

            _ => $"Política '{policy.Name}': acceso denegado"
        };
    }

    // ─── IP_RANGE ─────────────────────────────────────────────────────────────
    // Condiciones: IpRangeStart, IpRangeEnd

    private (bool matches, string reason) EvaluateIpRange(Policy policy, string ipAddress)
    {
        var conditions = policy.PolicyConditions.ToDictionary(c => c.ConditionKey, c => c.ConditionValue);

        if (!conditions.TryGetValue("IpRangeStart", out var startStr) ||
            !conditions.TryGetValue("IpRangeEnd", out var endStr))
            return (false, $"'{policy.Name}': condiciones IpRangeStart/IpRangeEnd incompletas");

        if (!IPAddress.TryParse(ipAddress, out var requestIp) ||
            !IPAddress.TryParse(startStr, out var startIp) ||
            !IPAddress.TryParse(endStr, out var endIp))
            return (false, $"'{policy.Name}': dirección IP inválida");

        var requestLong = IpToLong(requestIp);
        var startLong = IpToLong(startIp);
        var endLong = IpToLong(endIp);

        var inRange = requestLong >= startLong && requestLong <= endLong;
        return inRange
            ? (true, string.Empty)
            : (false, $"Politica: dirección IP no permitida");
    }

    private static long IpToLong(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
            System.Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    // ─── TIME_WINDOW ──────────────────────────────────────────────────────────
    // Condiciones obligatorias:
    //   StartTime   →  "08:00"
    //   EndTime     →  "16:00"
    //   Timezone    →  "UTC-4" | "America/Caracas" | "Venezuela Standard Time"
    //
    // Condición opcional:
    //   ApplyOnDays → días en que la política aplica, separados por coma.
    //                 "Monday,Tuesday,Wednesday,Thursday,Friday"
    //                 Valores: Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday
    //                 El efecto (ALLOW/DENY) lo define el campo Effect de la política.
    //                 Si no se indica, la política aplica todos los días.

    private (bool matches, string reason) EvaluateTimeWindow(Policy policy, DateTime utcNow)
    {
        var conditions = policy.PolicyConditions.ToDictionary(c => c.ConditionKey, c => c.ConditionValue);

        if (!conditions.TryGetValue("StartTime", out var startStr) ||
            !conditions.TryGetValue("EndTime", out var endStr) ||
            !conditions.TryGetValue("Timezone", out var timezoneId))
            return (false, $"'{policy.Name}': condiciones StartTime/EndTime/Timezone incompletas");

        if (!TimeSpan.TryParse(startStr, out var startTime) ||
            !TimeSpan.TryParse(endStr, out var endTime))
            return (false, $"'{policy.Name}': formato de hora inválido (use HH:mm)");
        var (tz, tzError) = ResolveTimezone(timezoneId.Trim());
        if (tz == null)
            return (false, $"'{policy.Name}': timezone '{timezoneId}' no reconocido — {tzError}");

        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

        // ── Validar día de la semana ──────────────────────────────────────────
        if (conditions.TryGetValue("ApplyOnDays", out var applyOnDaysStr) &&
            !string.IsNullOrWhiteSpace(applyOnDaysStr))
        {
            var (daysValid, policyDays, daysError) = ParseDays(applyOnDaysStr);
            if (!daysValid)
                return (false, $"'{policy.Name}': {daysError}");

            // Si el día actual NO está en los días configurados, la política no aplica
            if (!policyDays!.Contains(localNow.DayOfWeek))
                return (false, string.Empty);
        }
        // ─────────────────────────────────────────────────────────────────────

        var currentTime = localNow.TimeOfDay;

        bool inWindow;
        if (startTime <= endTime)
            inWindow = currentTime >= startTime && currentTime <= endTime;
        else
            // Rango que cruza medianoche: 22:00 – 06:00
            inWindow = currentTime >= startTime || currentTime <= endTime;

        return inWindow
            ? (true, string.Empty)
            : (false, $"Politica: acceso fuera del horario permitido [{startStr} - {endStr} {timezoneId}]. Hora local actual: {localNow:HH:mm}");
    }

    // Parsea "Monday,Tuesday,Friday" → HashSet<DayOfWeek>
    private static (bool valid, HashSet<DayOfWeek>? days, string error) ParseDays(string raw)
    {
        var result = new HashSet<DayOfWeek>();
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (!Enum.TryParse<DayOfWeek>(part, ignoreCase: true, out var day))
                return (false, null, $"día '{part}' no reconocido. Use: Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday");

            result.Add(day);
        }

        return result.Count == 0
            ? (false, null, "ApplyOnDays está vacío")
            : (true, result, string.Empty);
    }

    // ─── Resolución de timezone con soporte multi-formato ────────────────────
    //
    // Prioridad:
    //   1. Offset raw:  "UTC-4" | "UTC+5:30" | "-04:00" | "+05:30"
    //   2. IANA ID:     "America/Caracas"
    //   3. Windows ID:  "Venezuela Standard Time"

    private static (TimeZoneInfo? tz, string error) ResolveTimezone(string timezoneId)
    {
        // 1. Intentar parsear como offset UTC
        var offset = ParseUtcOffset(timezoneId);
        if (offset.HasValue)
            return (TimeZoneInfo.CreateCustomTimeZone(
                id: timezoneId,
                baseUtcOffset: offset.Value,
                displayName: timezoneId,
                standardDisplayName: timezoneId), string.Empty);

        // 2. Intentar IANA o Windows ID directamente
        try
        {
            return (TimeZoneInfo.FindSystemTimeZoneById(timezoneId), string.Empty);
        }
        catch { }

        // 3. En Windows: convertir IANA → Windows y reintentar
        try
        {
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timezoneId, out var windowsId))
                return (TimeZoneInfo.FindSystemTimeZoneById(windowsId!), string.Empty);
        }
        catch { }

        // 4. En Linux/Mac: convertir Windows → IANA y reintentar
        try
        {
            if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timezoneId, out var ianaId))
                return (TimeZoneInfo.FindSystemTimeZoneById(ianaId!), string.Empty);
        }
        catch { }

        return (null, $"No se pudo resolver el timezone '{timezoneId}'. " +
                      "Use formato UTC±H, UTC±H:mm, offset ±HH:mm, IANA ID o Windows ID.");
    }

    // Parsea los formatos de offset:
    //   "UTC-4"    "UTC+5"    "UTC-4:30"    "UTC+05:30"
    //   "-04:00"   "+05:30"   "-4"          "+5:30"
    private static TimeSpan? ParseUtcOffset(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var raw = value.Trim();

        // Quitar prefijo "UTC" si lo tiene
        if (raw.StartsWith("UTC", StringComparison.OrdinalIgnoreCase))
            raw = raw[3..].Trim();

        // Ahora raw debe ser: "+5", "-4", "+5:30", "-04:00", etc.
        if (raw.Length == 0) return TimeSpan.Zero; // "UTC" sólo = UTC+0

        if (raw[0] != '+' && raw[0] != '-') return null;

        var sign = raw[0] == '+' ? 1 : -1;
        var parts = raw[1..].Split(':');

        if (!int.TryParse(parts[0], out var hours)) return null;
        var minutes = 0;
        if (parts.Length > 1 && !int.TryParse(parts[1], out minutes)) return null;

        if (hours is < 0 or > 14 || minutes is < 0 or > 59) return null;

        return new TimeSpan(sign * hours, sign * minutes, 0);
    }

    // ─── Log de auditoría ─────────────────────────────────────────────────────

    private async Task SaveLogAsync(
        UserApplication userApp, Policy policy, string result,
        string? failureReason, string? ipAddress, string? userAgent)
    {
        var log = new PolicyEvaluationLog
        {
            PolicyEvaluationLogId = Guid.NewGuid(),
            UserId = userApp.UserId,
            PolicyId = policy.PolicyId,
            OrganizationId = userApp.OrganizationApplication.OrganizationId,
            EvaluationResult = result,
            FailureReason = failureReason,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            EvaluatedAt = DateTime.UtcNow
        };

        await _dbContext.Set<PolicyEvaluationLog>().AddAsync(log);
        await _dbContext.SaveChangesAsync();
    }
}
