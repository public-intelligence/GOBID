using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using System.Text.Json;

namespace GOBID.Api.Middlewares;

public class PolicyEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    public PolicyEnforcementMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _scopeFactory = scopeFactory;
    }

    public async Task Invoke(HttpContext context)
    {
        // Solo evaluar peticiones autenticadas
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userAppIdClaim = context.User.FindFirst("user_application_id")?.Value;

            // Solo evaluar si el token tiene el claim user_application_id (Guid)
            if (!string.IsNullOrEmpty(userAppIdClaim) && Guid.TryParse(userAppIdClaim, out var userApplicationId))
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString()
                    ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                    ?? "unknown";

                var userAgent = context.Request.Headers.UserAgent.ToString();

                using var scope = _scopeFactory.CreateScope();
                var policyEvaluator = scope.ServiceProvider.GetRequiredService<IPolicyEvaluatorService>();

                var result = await policyEvaluator.EvaluateAsync(userApplicationId, ipAddress, userAgent);

                if (!result.Succeeded || result.Data?.IsAllowed == false)
                {
                    // Revocar la sesión para forzar el cierre de sesión en el cliente
                    var sessionIdClaim = context.User.FindFirst("session_id")?.Value;
                    if (!string.IsNullOrEmpty(sessionIdClaim) && Guid.TryParse(sessionIdClaim, out var sessionId))
                    {
                        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                        await authService.RevokeSessionAsync(sessionId);
                    }

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var reason = !string.IsNullOrWhiteSpace(result.Data?.Reason)
                        ? result.Data!.Reason
                        : !string.IsNullOrWhiteSpace(result.Message)
                            ? result.Message
                            : "Acceso denegado por política de seguridad";

                    var body = JsonSerializer.Serialize(new Response<object>(reason, false));

                    await context.Response.WriteAsync(body);
                    return;
                }
            }
        }

        await _next(context);
    }
}
