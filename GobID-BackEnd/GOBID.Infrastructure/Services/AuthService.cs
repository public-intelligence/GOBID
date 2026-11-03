using Dapper;
using GOBID.Application.DTOs;
using GOBID.Application.DTOs.ApplicationCredential;
using GOBID.Application.DTOs.Auth;
using GOBID.Application.DTOs.Personnel;
using GOBID.Application.DTOs.User;
using GOBID.Application.DTOs.UserApplication;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.OrganizationApplication;
using GOBID.Infrastructure.Security;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using GOBID.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Claim = System.Security.Claims.Claim;
using ClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace GOBID.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IDbConnection _connection;
    private readonly IConfiguration _config;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly PrincipalContext _context;
    private readonly IPersonApiService _personService;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPolicyEvaluatorService _policyEvaluator;

    public AuthService(
        IDbConnection connection,
        IConfiguration config,
        IUserService userService,
        IEmailService emailService,
        PrincipalContext context,
        IPersonApiService personService,
        ILogger<AuthService> logger,
        IHttpContextAccessor httpContextAccessor,
        IPolicyEvaluatorService policyEvaluator)
    {
        _connection = connection;
        _config = config;
        _userService = userService;
        _emailService = emailService;
        _context = context;
        _personService = personService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _policyEvaluator = policyEvaluator;
    }

    public class ClaimsWrapper
    {
        [JsonProperty("data")]
        public List<ClaimData>? Data { get; set; }

        [JsonProperty("claims")]
        public List<ClaimData>? Claims { get; set; }
    }

    public class ClaimData
    {
        [JsonProperty("claim_code")]
        public string ClaimCode { get; set; } = string.Empty;
    }

    public async Task<Response<LoginResultDto>> LoginJwtAsync(LoginRequestDto dto)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            
            var payload = JsonConvert.SerializeObject(dto, settings);

            var loginResultJson = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_authenticate_user",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrWhiteSpace(loginResultJson))
            {
                await WriteAuditAsync(SecurityAuditEventTypes.LoginFailure, false, null, dto.ApplicationId,
                    JsonConvert.SerializeObject(new { login = dto.Login, reason = "user_not_found" }, settings));
                return new Response<LoginResultDto>("Credenciales inv\u00e1lidas", false);
            }

            var loginResult = JsonConvert.DeserializeObject<LoginResultDto>(loginResultJson, settings);
            if (loginResult == null)
            {
                await WriteAuditAsync(SecurityAuditEventTypes.LoginFailure, false, null, dto.ApplicationId,
                    JsonConvert.SerializeObject(new { login = dto.Login, reason = "parse_error" }, settings));
                return new Response<LoginResultDto>("Credenciales inválidas", false);
            }

            var userWithUnconfirmedEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == loginResult.UserId && u.IsActive && u.EmailConfirmed == false && u.EmailConfirmationToken != null);

            if (loginResult is null || !SecretHasher.Verify(dto.Password, loginResult.UserPassword))
            {
                await WriteAuditAsync(SecurityAuditEventTypes.LoginFailure, false, loginResult?.UserId, loginResult?.ApplicationId,
                    JsonConvert.SerializeObject(new { login = dto.Login, reason = "invalid_password" }, settings));
                return new Response<LoginResultDto>("Credenciales inv\u00e1lidas", false);
            }

            if (userWithUnconfirmedEmail != null)
            {
                var storedJwt = SafeReadToken(userWithUnconfirmedEmail.EmailConfirmationToken ?? string.Empty);
                bool tokenExpired = storedJwt == null || storedJwt.ValidTo < DateTime.UtcNow;

                if (tokenExpired)
                {
                    var newConfirmToken = await GenerateEmailConfirmationTokenAsync(loginResult.UserId, userWithUnconfirmedEmail.Email);
                    userWithUnconfirmedEmail.EmailConfirmationToken = newConfirmToken;
                    await _context.SaveChangesAsync();

                    var clientBaseUrl = _config["ClientBaseUrl"]?.TrimEnd('/') ?? string.Empty;
                    var confirmationLink = $"{clientBaseUrl}/verify/email?token={Uri.EscapeDataString(newConfirmToken)}";
                    try
                    {
                        await _emailService.SendEmailConfirmationAsync(userWithUnconfirmedEmail.Email, userWithUnconfirmedEmail.Email, confirmationLink);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Error al reenviar email de confirmaci\u00f3n a {Email}", userWithUnconfirmedEmail.Email);
                    }
                }

                await WriteAuditAsync(SecurityAuditEventTypes.LoginFailure, false, loginResult.UserId, loginResult.ApplicationId,
                    JsonConvert.SerializeObject(new { login = dto.Login, reason = "email_not_confirmed" }, settings));

                var confirmMsg = tokenExpired
                    ? "Debe confirmar su correo electr\u00f3nico. Se ha enviado un nuevo enlace de confirmaci\u00f3n."
                    : "Debe confirmar el correo para poder iniciar sesi\u00f3n.";

                return new Response<LoginResultDto>(confirmMsg, false);
            }

            loginResult.UserPassword = null;
            loginResult.IsTrustedDevice = dto.IsTrustedDevice;

            await WriteAuditAsync(SecurityAuditEventTypes.LoginSuccess, true, loginResult.UserId, loginResult.ApplicationId,
                JsonConvert.SerializeObject(new { login = dto.Login }, settings));

            return new Response<LoginResultDto>
            {
                Succeeded = true,
                Message = "Inicio de sesi\u00f3n exitoso",
                Data = loginResult
            };
        }
        catch (Exception ex)
        {
            return new Response<LoginResultDto>($"Error al autenticar al usuario: {ex.Message}", false);
        }
    }

    public async Task<string> GenerateJwtTokenAsync(LoginResultDto loginResult)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, loginResult.UserId.ToString()),
            new Claim("person_id", loginResult.PersonnelId.ToString()),
            new Claim("application_id", loginResult.ApplicationId.ToString()),
            new Claim("gobid_logo_url", _config["GOBIDLogoUrl"] ?? string.Empty)
        };

        if (loginResult.PersonPositionId.HasValue)
            claims.Add(new Claim("personnel_id", loginResult.PersonPositionId.Value.ToString()));

        if (loginResult.OrgContextSelected)
        {
            claims.Add(new Claim("organization_application_id", loginResult.OrganizationApplicationId.ToString()));
            claims.Add(new Claim("organization_id", loginResult.OrganizationId.ToString()));
            claims.Add(new Claim("organization_name", loginResult.OrganizationName));
            claims.Add(new Claim("organization_acronym", loginResult.Acronym));
            claims.Add(new Claim("role_name", loginResult.RoleName));

            if (loginResult.RoleId.HasValue)
                claims.Add(new Claim("role_id", loginResult.RoleId.Value.ToString()));

            if (loginResult.UserApplicationId.HasValue)
                claims.Add(new Claim("user_application_id", loginResult.UserApplicationId.Value.ToString()));
        }

        var personnelResponse = await _personService.GetPersonByIdAsync(loginResult.PersonnelId);
        if (personnelResponse.Succeeded && personnelResponse.Data != null)
        {
            var fullName = $"{personnelResponse.Data.FirstName} {personnelResponse.Data.LastName}".Trim();
            if (!string.IsNullOrEmpty(fullName))
                claims.Add(new Claim("person_full_name", fullName));
        }

        if (loginResult.OrgContextSelected && loginResult.Claims != null)
        {
            var permissions = loginResult.Claims
                .Where(c => !string.IsNullOrWhiteSpace(c.ClaimCode))
                .Select(c => c.ClaimCode!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (permissions.Length > 0)
            {
                string permissionsJson = JsonConvert.SerializeObject(permissions);
                claims.Add(new Claim("permissions", permissionsJson));
            }
        }

        var signingKey = await GetSigningKeyForApp(loginResult.ApplicationId);
        signingKey.KeyId = $"app:{loginResult.ApplicationId}";

        if (loginResult.SessionId.HasValue)
            claims.Add(new Claim("session_id", loginResult.SessionId.Value.ToString()));

        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_config.GetValue<int>("Jwt:AccessTokenExpiryHours", 1)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<Response<string>> GenerateAuthCodeAsync(LoginResultDto loginResult)
    {
        try
        {
            var rawCodeBytes = RandomNumberGenerator.GetBytes(32);
            var rawCode = Convert.ToBase64String(rawCodeBytes)
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawCode));
            var codeHash = Convert.ToHexString(hashBytes).ToLower();

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.ApplicationId == loginResult.ApplicationId);

            if (application == null)
                return new Response<string>("Aplicaci\u00f3n no encontrada", false);

            var authData = JsonConvert.SerializeObject(loginResult);

            var authCode = new AuthorizationCode
            {
                CodeId = Guid.NewGuid(),
                CodeHash = codeHash,
                UserId = loginResult.UserId,
                ApplicationId = loginResult.ApplicationId,
                RedirectUri = application.RedirectUrl ?? string.Empty,
                AuthData = authData,
                IsUsed = false,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                CreatedAt = DateTime.UtcNow
            };

            _context.AuthorizationCodes.Add(authCode);
            await _context.SaveChangesAsync();

            return new Response<string> { Succeeded = true, Data = rawCode };
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al generar el c\u00f3digo de autorizaci\u00f3n: {ex.Message}", false);
        }
    }

    public async Task<Response<TokenPairDto>> ExchangeCodeForTokensAsync(ExchangeCodeDto dto)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dto.Code));
            var codeHash = Convert.ToHexString(hashBytes).ToLower();

            var authCode = await _context.AuthorizationCodes
                .FirstOrDefaultAsync(c => c.CodeHash == codeHash);

            if (authCode == null)
                return new Response<TokenPairDto>("Código de autorización inválido", false);

            if (authCode.IsUsed)
                return new Response<TokenPairDto>("El código de autorización ya fue utilizado", false);

            if (authCode.ExpiresAt < DateTime.UtcNow)
                return new Response<TokenPairDto>("El código de autorización ha expirado", false);

            if (!string.Equals(authCode.RedirectUri, dto.RedirectUri, StringComparison.OrdinalIgnoreCase))
                return new Response<TokenPairDto>("El redirect_uri no coincide", false);

            authCode.IsUsed = true;
            await _context.SaveChangesAsync();

            var loginResult = JsonConvert.DeserializeObject<LoginResultDto>(authCode.AuthData ?? "{}");
            if (loginResult == null)
                return new Response<TokenPairDto>("Error al recuperar los datos de autenticación", false);

            // ── Evaluación de políticas ABAC (primer filtro) ──────────────────
            if (dto.OrganizationApplicationId > 0)
            {
                var userApp = await _context.UserApplications
                    .FirstOrDefaultAsync(ua => ua.UserId == loginResult.UserId
                                            && ua.OrganizationApplicationId == dto.OrganizationApplicationId
                                            && ua.IsActive);

                if (userApp != null)
                {
                    loginResult.UserApplicationId = userApp.UserApplicationId;

                    var evalResult = await _policyEvaluator.EvaluateAsync(
                        userApp.UserApplicationId, GetClientIp(), GetUserAgent());

                    if (!evalResult.Succeeded)
                        return new Response<TokenPairDto>($"Error al evaluar políticas: {evalResult.Message}", false)
                        {
                            StatusCode = 500
                        };

                    if (!evalResult.Data.IsAllowed)
                        return new Response<TokenPairDto>($"Acceso denegado por política de seguridad: {evalResult.Data.Reason}", false)
                        {
                            StatusCode = 403
                        };
                }
            }
            // ─────────────────────────────────────────────────────────────────

            if (dto.OrganizationApplicationId > 0)
            {
                var orgContextSettings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                };
                var orgContextFilter = JsonConvert.SerializeObject(new
                {
                    user_id = loginResult.UserId,
                    organization_application_id = dto.OrganizationApplicationId,
                    application_id = loginResult.ApplicationId,
                    is_active = true
                }, orgContextSettings);

                var orgContextJson = await _connection.QueryFirstOrDefaultAsync<string>(
                    "read_user_org_context",
                    new { filter = orgContextFilter },
                    commandType: CommandType.StoredProcedure);

                if (string.IsNullOrEmpty(orgContextJson) || orgContextJson == "{}")
                {
                    // No tiene UserApplication — verificar si la app es pública para otorgar permisos automáticos
                    var publicOrgApp = await _context.OrganizationApplications
                        .Include(oa => oa.Application)
                        .Include(oa => oa.Organization)
                        .FirstOrDefaultAsync(oa =>
                            oa.OrganizationApplicationId == dto.OrganizationApplicationId &&
                            oa.Application.IsPublic &&
                            oa.IsActive);

                    if (publicOrgApp == null)
                        return new Response<TokenPairDto>("El usuario no tiene acceso a esta organización-aplicación", false);

                    var automaticClaims = await _context.Claims
                        .Where(c =>
                            c.ApplicationId == loginResult.ApplicationId &&
                            c.IsAutomatic &&
                            c.IsActive)
                        .Select(c => c.Code)
                        .Distinct()
                        .ToListAsync();

                    loginResult.OrganizationApplicationId = dto.OrganizationApplicationId;
                    loginResult.OrganizationId = publicOrgApp.OrganizationId;
                    loginResult.OrganizationName = publicOrgApp.Organization?.OrganizationName ?? string.Empty;
                    loginResult.Acronym = publicOrgApp.Organization?.Acronym ?? string.Empty;
                    loginResult.OrgContextSelected = true;
                    loginResult.Claims = automaticClaims
                        .Select(code => new ClaimDto { ClaimCode = code })
                        .ToList();
                }
                else
                {
                    var orgContext = JsonConvert.DeserializeObject<OrgContextRow>(orgContextJson);
                    if (orgContext == null)
                        return new Response<TokenPairDto>("El usuario no tiene acceso a esta organización-aplicación", false);

                    loginResult.OrganizationApplicationId = dto.OrganizationApplicationId;
                    loginResult.OrganizationId = orgContext.OrganizationId;
                    loginResult.OrganizationName = orgContext.OrganizationName ?? string.Empty;
                    loginResult.Acronym = orgContext.Acronym ?? string.Empty;
                    loginResult.OrgContextSelected = true;
                    loginResult.RoleName = orgContext.RoleName ?? string.Empty;
                    loginResult.RoleId = orgContext.RoleId;
                    loginResult.Claims = [];

                    var claimsResponse = await GetClaimsAsync(loginResult.UserId, dto.OrganizationApplicationId);
                    if (claimsResponse.Succeeded && claimsResponse.Data != null && !string.IsNullOrEmpty(claimsResponse.Data.ClaimCode))
                    {
                        loginResult.Claims = claimsResponse.Data.ClaimCode
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(code => new ClaimDto { ClaimCode = code })
                            .ToList();
                    }
                }
            }

            await MergePositionRoleClaimsAsync(loginResult);

            // ─────────────────────────────────────────────────────────────────

            var accessTokenTask = GenerateJwtTokenAsync(loginResult);
            var refreshTask = CreateRefreshTokenAsync(loginResult);
            await Task.WhenAll(accessTokenTask, refreshTask);

            var refreshResult = refreshTask.Result;
            if (!refreshResult.Succeeded)
                return new Response<TokenPairDto>(refreshResult.Message ?? string.Empty, false);

            if (loginResult.OrgContextSelected && loginResult.OrganizationId > 0)
            {
                try
                {
                    _context.LoginLogs.Add(new LoginLog
                    {
                        LoginInfo = loginResult.UserId.ToString(),
                        Ip = GetClientIp(),
                        HeaderValues = GetUserAgent(),
                        LoginSuccessful = true,
                        OrganizationId = loginResult.OrganizationId,
                        OrganizationApplicationId = loginResult.OrganizationApplicationId,
                        IsActive = true,
                        CorrelationId = GetCorrelationId(),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = loginResult.UserId
                    });
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error writing login log for user {UserId}", loginResult.UserId);
                }
            }

            return new Response<TokenPairDto>
            {
                Succeeded = true,
                Message = "Tokens generados correctamente",
                Data = new TokenPairDto
                {
                    AccessToken = accessTokenTask.Result,
                    RefreshToken = refreshResult.Data!,
                    MustChangePassword = loginResult.MustChangePassword,
                    SessionId = loginResult.SessionId
                }
            };
        }
        catch (Exception ex)
        {
            return new Response<TokenPairDto>($"Error al intercambiar el código: {ex.Message}", false);
        }
    }

    private async Task<Response<string>> CreateRefreshTokenAsync(
        LoginResultDto loginResult,
        Guid? existingSessionId = null)
    {
        try
        {
            var rawTokenBytes = RandomNumberGenerator.GetBytes(32);
            var rawToken = Convert.ToBase64String(rawTokenBytes)
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
            var tokenHash = Convert.ToHexString(hashBytes).ToLower();

            Guid sessionId;
            if (existingSessionId.HasValue)
            {
                sessionId = existingSessionId.Value;
                var existingSession = await _context.UserSessions.FindAsync(sessionId);
                if (existingSession != null)
                {
                    existingSession.LastActivityAt = DateTime.UtcNow;
                    loginResult.IsTrustedDevice = existingSession.IsTrusted;
                }
            }
            else
            {
                var currentIp = GetClientIp();
                var currentUserAgent = GetUserAgent();

                var existingSession = await _context.UserSessions
                    .FirstOrDefaultAsync(s => s.UserId        == loginResult.UserId
                                           && s.ApplicationId == loginResult.ApplicationId
                                           && s.Ip            == currentIp
                                           && s.UserAgent     == currentUserAgent
                                           && s.IsActive);

                var isKnownDevice = existingSession != null;

                if (existingSession != null)
                {
                    existingSession.LastActivityAt    = DateTime.UtcNow;
                    existingSession.AbsoluteExpiresAt = DateTime.UtcNow.AddDays(
                        loginResult.IsTrustedDevice
                            ? _config.GetValue<int>("Session:AbsoluteExpiryDays", 30)
                            : _config.GetValue<int>("Session:RefreshTokenExpiryDays", 7));
                    sessionId = existingSession.SessionId;
                }
                else
                {
                    var absoluteExpiryDays = loginResult.IsTrustedDevice
                        ? _config.GetValue<int>("Session:AbsoluteExpiryDays", 30)
                        : _config.GetValue<int>("Session:RefreshTokenExpiryDays", 7);
                    var newSession = new UserSession
                    {
                        SessionId        = Guid.NewGuid(),
                        UserId           = loginResult.UserId,
                        ApplicationId    = loginResult.ApplicationId,
                        Ip               = currentIp,
                        UserAgent        = currentUserAgent,
                        IsTrusted        = loginResult.IsTrustedDevice,
                        IsActive         = true,
                        CreatedAt        = DateTime.UtcNow,
                        LastActivityAt   = DateTime.UtcNow,
                        AbsoluteExpiresAt = DateTime.UtcNow.AddDays(absoluteExpiryDays)
                    };
                    _context.UserSessions.Add(newSession);
                    sessionId = newSession.SessionId;
                }

                if (!isKnownDevice)
                {
                    var userEmail = await _context.Users
                        .Where(u => u.UserId == loginResult.UserId)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        var applicationName = !string.IsNullOrWhiteSpace(loginResult.ApplicationName)
                            ? loginResult.ApplicationName
                            : await _context.Applications
                                .Where(a => a.ApplicationId == loginResult.ApplicationId)
                                .Select(a => a.ApplicationName)
                                .FirstOrDefaultAsync() ?? "GOBID";

                        var userAgentDisplay = !string.IsNullOrWhiteSpace(currentUserAgent)
                            ? currentUserAgent
                            : "No especificado";

                        var loginDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _emailService.SendNewDeviceLoginAsync(userEmail, userEmail, currentIp, userAgentDisplay, loginDateTime, applicationName);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error al enviar notificaci\u00f3n de nuevo dispositivo a {Email}", userEmail);
                            }
                        });
                    }
                }
            }

            loginResult.SessionId = sessionId;

            var expiryDays = loginResult.IsTrustedDevice
                ? _config.GetValue<int>("Session:TrustedDeviceExpiryDays", 30)
                : _config.GetValue<int>("Session:RefreshTokenExpiryDays", 7);

            var refreshToken = new RefreshToken
            {
                TokenId = Guid.NewGuid(),
                TokenHash = tokenHash,
                UserId = loginResult.UserId,
                ApplicationId = loginResult.ApplicationId,
                AuthData = JsonConvert.SerializeObject(loginResult),
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                CreatedAt = DateTime.UtcNow,
                CorrelationId = GetCorrelationId(),
                SessionId = sessionId
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            await WriteAuditAsync(SecurityAuditEventTypes.TokenIssued, true, loginResult.UserId, loginResult.ApplicationId,
                JsonConvert.SerializeObject(new { token_id = refreshToken.TokenId, session_id = sessionId }));

            return new Response<string> { Succeeded = true, Data = rawToken };
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al crear el refresh token: {ex.Message}", false);
        }
    }

    public async Task<Response<TokenPairDto>> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
            var tokenHash = Convert.ToHexString(hashBytes).ToLower();

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

            if (storedToken == null)
                return new Response<TokenPairDto>("Refresh token inv\u00e1lido", false);

            if (storedToken.IsRevoked)
                return new Response<TokenPairDto>("El refresh token ha sido revocado", false);

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return new Response<TokenPairDto>("El refresh token ha expirado", false);

            if (storedToken.SessionId.HasValue)
            {
                var session = await _context.UserSessions.FindAsync(storedToken.SessionId.Value);
                if (session != null)
                {
                    if (!session.IsActive)
                        return new Response<TokenPairDto>("La sesi\u00f3n ha sido revocada. Por favor inicie sesi\u00f3n nuevamente.", false);
                    if (session.AbsoluteExpiresAt < DateTime.UtcNow)
                    {
                        session.IsActive = false;
                        await _context.SaveChangesAsync();
                        return new Response<TokenPairDto>("La sesi\u00f3n ha expirado. Por favor inicie sesi\u00f3n nuevamente.", false);
                    }
                }
            }

            var loginResult = JsonConvert.DeserializeObject<LoginResultDto>(storedToken.AuthData ?? "{}");
            if (loginResult == null)
                return new Response<TokenPairDto>("Error al recuperar los datos de autenticaci\u00f3n", false);

            if (loginResult.OrgContextSelected && loginResult.OrganizationApplicationId > 0)
            {
                var orgContextSettings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                };

                var orgContextFilter = JsonConvert.SerializeObject(new
                {
                    user_id = loginResult.UserId,
                    organization_application_id = loginResult.OrganizationApplicationId,
                    application_id = loginResult.ApplicationId,
                    is_active = true
                }, orgContextSettings);

                var orgContextJson = await _connection.QueryFirstOrDefaultAsync<string>(
                    "read_user_org_context",
                    new { filter = orgContextFilter },
                    commandType: CommandType.StoredProcedure);

                if (!string.IsNullOrEmpty(orgContextJson) && orgContextJson != "{}")
                {
                    var orgContext = JsonConvert.DeserializeObject<OrgContextRow>(orgContextJson);
                    if (orgContext != null)
                    {
                        loginResult.RoleName = orgContext.RoleName ?? string.Empty;
                        loginResult.RoleId = orgContext.RoleId;
                    }
                }
            }

            // ── Re-evaluación de políticas ABAC en cada refresh ───────────────
            if (loginResult.OrgContextSelected && loginResult.UserApplicationId.HasValue)
            {
                var clientIp = GetClientIp();
                var userAgent = GetUserAgent();
                var evalResult = await _policyEvaluator.EvaluateAsync(loginResult.UserApplicationId.Value, clientIp, userAgent);

                if (!evalResult.Succeeded)
                    return new Response<TokenPairDto>($"Error al evaluar políticas: {evalResult.Message}", false)
                    {
                        StatusCode = 500
                    };

                if (!evalResult.Data.IsAllowed)
                    return new Response<TokenPairDto>($"Acceso denegado por política de seguridad: {evalResult.Data.Reason}", false)
                    {
                        StatusCode = 403
                    };
            }
            // ─────────────────────────────────────────────────────────────────

            await MergePositionRoleClaimsAsync(loginResult);

            var accessToken = await GenerateJwtTokenAsync(loginResult);
            var newRefreshResult = await CreateRefreshTokenAsync(loginResult, storedToken.SessionId);
            if (!newRefreshResult.Succeeded)
                return new Response<TokenPairDto>(newRefreshResult.Message ?? string.Empty, false);

            using var sha256New = SHA256.Create();
            var newHashBytes = sha256New.ComputeHash(Encoding.UTF8.GetBytes(newRefreshResult.Data!));
            var newTokenHash = Convert.ToHexString(newHashBytes).ToLower();
            var newStoredToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == newTokenHash);

            storedToken.IsRevoked = true;
            storedToken.ReplacedBy = newStoredToken?.TokenId;
            await _context.SaveChangesAsync();

            await WriteAuditAsync(SecurityAuditEventTypes.TokenRefreshed, true, loginResult.UserId, loginResult.ApplicationId,
                JsonConvert.SerializeObject(new { old_token_id = storedToken.TokenId, new_token_id = newStoredToken?.TokenId }));

            return new Response<TokenPairDto>
            {
                Succeeded = true,
                Message = "Tokens refrescados correctamente",
                Data = new TokenPairDto
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshResult.Data!,
                    MustChangePassword = loginResult.MustChangePassword
                }
            };
        }
        catch (Exception ex)
        {
            return new Response<TokenPairDto>($"Error al refrescar los tokens: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            if (dto.NewPassword != dto.ConfirmNewPassword)
                return new Response<string>("Las contrase\u00f1as nuevas no coinciden", false);

            await _userService.GetByIdAsync(userId);
            var payload = JsonConvert.SerializeObject(new
            {
                user_id = userId,
                new_password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword),
                must_change_password = dto.MustChangePassword
            }, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "update_user_password",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (result != null)
                return new Response<string>("No se pudo cambiar la contrase\u00f1a", false);
            return new Response<string>("Contrase\u00f1a cambiada exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al cambiar la contrase\u00f1a: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> RequestPasswordResetAsync(ForgotPasswordRequestDto dto)
    {
        try
        {
            var users = await _userService.ListAsync(1, 1, new UserFilterDto { EmailLike = dto.Email }, organizationId: null);
            if (users == null || !users.Succeeded || users.Data == null || users.Data.Count == 0)
                return new Response<string>("Si el correo existe, enviaremos un enlace de restablecimiento.", true);

            var user = users.Data.First();

            var key = await GetSigningKeyForApp(dto.ApplicationId);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim("application_id", dto.ApplicationId.ToString()),
                new Claim("purpose", "pwd_reset")
            };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:PasswordResetTokenExpiryMinutes", 30)),
                signingCredentials: creds
            );
            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            var baseUrl = _config["Frontend:BaseUrl"] ?? _config["Frontend:ResetPasswordUrl"] ?? string.Empty;
            var root = baseUrl.TrimEnd('/');
            var resetLink = $"{root}/app/{dto.ApplicationId}/change-password?token={Uri.EscapeDataString(tokenStr)}";

            var response = new Response<string>("Si el correo existe, enviaremos un enlace de restablecimiento.", true);
            try
            {
                await _emailService.SendResetPasswordEmailAsync(dto.Email, dto.Email, resetLink);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar email de restablecimiento a {Email}: {Message}", dto.Email, ex.Message);
                return response;
            }
        }
        catch (Exception ex)
        {
            return new Response<string>("Error al solicitar restablecimiento", ex.Message);
        }
    }

    public async Task<Response<string>> ResetPasswordAsync(ResetPasswordDto dto)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = SafeReadToken(dto.Token);
            var appId = TryGetApplicationId(jwt);
            if (appId == null)
                return new Response<string>("Token inv\u00e1lido: sin application_id.", false);

            var key = await GetSigningKeyForApp(appId.Value);
            ClaimsPrincipal principal;
            try
            {
                principal = handler.ValidateToken(dto.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _config["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);
            }
            catch (SecurityTokenExpiredException)
            {
                return new Response<string>("El enlace ha expirado.", false);
            }
            catch (Exception ex)
            {
                return new Response<string>($"Token inv\u00e1lido: {ex.Message}", false);
            }

            var purpose = principal.Claims.FirstOrDefault(c => c.Type == "purpose")?.Value;
            if (!string.Equals(purpose, "pwd_reset", StringComparison.Ordinal))
                return new Response<string>("Token inv\u00e1lido para restablecer contrase\u00f1a.", false);

            var userIdStr = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
                            ?? principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return new Response<string>("Usuario inv\u00e1lido en el token.", false);

            var resp = await _userService.ChangePasswordAsync(userId, new ChangePasswordDto
            {
                NewPassword = dto.NewPassword,
                ConfirmNewPassword = dto.ConfirmNewPassword,
                MustChangePassword = false
            });

            return resp;
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al restablecer la contrase\u00f1a: {ex.Message}", false);
        }
    }

    public async Task<Response<PersonnelInfoDto>> GetPersonnelInfoAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtIssuer = _config["Jwt:Issuer"];
            var jwtAudience = _config["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtIssuer))
                return new Response<PersonnelInfoDto>("El emisor JWT no est\u00e1 configurado.", false);
            if (string.IsNullOrWhiteSpace(jwtAudience))
                return new Response<PersonnelInfoDto>("La audiencia JWT no est\u00e1 configurada.", false);

            var readToken = SafeReadToken(token);
            var appId = TryGetApplicationId(readToken);
            if (appId == null)
                return new Response<PersonnelInfoDto>("El token no contiene 'application_id'.", false);

            var signingKey = await GetSigningKeyForApp(appId.Value);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = validatedToken as JwtSecurityToken;
                if (jwtToken == null)
                    return new Response<PersonnelInfoDto>("Token inv\u00e1lido.", false);

                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
                                  ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var personnelIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "personnel_id")?.Value;

                if (string.IsNullOrEmpty(personnelIdClaim) || !Guid.TryParse(personnelIdClaim, out var personnelId))
                    return new Response<PersonnelInfoDto>("ID de personal no v\u00e1lido en el token", false);

                Guid? userId = null;
                if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
                    userId = parsedUserId;

                var personResponse = await _personService.GetPersonByIdAsync(personnelId);
                if (personResponse == null || !personResponse.Succeeded || personResponse.Data == null)
                    return new Response<PersonnelInfoDto>("Informaci\u00f3n de personal no encontrada", false);

                var person = personResponse.Data;
                var personnelInfo = new PersonnelInfoDto
                {
                    UserId = userId,
                    PersonnelId = personnelId,
                    FirstName = person.FirstName,
                    LastName = person.LastName,
                    IdentificationNumber = person.IdentificationNumber,
                    Email = person.Email,
                    Position = string.Empty,
                    OrganizationalStructure = string.Empty,
                    OccupationalGroup = string.Empty,
                    AreaType = string.Empty,
                    AreaId = string.Empty,
                    TopAreaId = string.Empty,
                    TopAreaName = string.Empty
                };

                return new Response<PersonnelInfoDto>
                {
                    Succeeded = true,
                    Data = personnelInfo
                };
            }
            catch (SecurityTokenExpiredException)
            {
                return new Response<PersonnelInfoDto>("El token ha expirado.", false);
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return new Response<PersonnelInfoDto>("La firma del token no es v\u00e1lida.", false);
            }
            catch (SecurityTokenException ex)
            {
                return new Response<PersonnelInfoDto>($"Token inv\u00e1lido: {ex.Message}", false);
            }
            catch (Exception ex)
            {
                return new Response<PersonnelInfoDto>($"Error al validar el token: {ex.Message}", false);
            }
        }
        catch (Exception ex)
        {
            return new Response<PersonnelInfoDto>($"Error al obtener la informaci\u00f3n del personal: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> VerifyTokenAsync(string token)
    {
        try
        {
            return await Task.Run(async () =>
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtIssuer = _config["Jwt:Issuer"];
                var jwtAudience = _config["Jwt:Audience"];

                if (string.IsNullOrWhiteSpace(jwtIssuer))
                    return new Response<string>("El emisor JWT no est\u00e1 configurado.", false);
                if (string.IsNullOrWhiteSpace(jwtAudience))
                    return new Response<string>("La audiencia JWT no est\u00e1 configurada.", false);

                var readToken = SafeReadToken(token);
                var appId = TryGetApplicationId(readToken);
                if (appId == null)
                    return new Response<string>("El token no contiene 'application_id'.", false);

                var signingKey = await GetSigningKeyForApp(appId.Value);

                try
                {
                    tokenHandler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    }, out SecurityToken _);

                    return new Response<string>
                    {
                        Succeeded = true,
                        Message = "Token v\u00e1lido"
                    };
                }
                catch (SecurityTokenExpiredException)
                {
                    return new Response<string>("El token ha expirado.", false);
                }
                catch (SecurityTokenInvalidSignatureException)
                {
                    return new Response<string>("La firma del token no es v\u00e1lida.", false);
                }
                catch (SecurityTokenException ex)
                {
                    return new Response<string>($"Token inv\u00e1lido: {ex.Message}", false);
                }
                catch (Exception ex)
                {
                    return new Response<string>($"Error al validar el token: {ex.Message}", false);
                }
            });
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error inesperado al verificar el token: {ex.Message}", false);
        }
    }

    private async Task MergePositionRoleClaimsAsync(LoginResultDto loginResult)
    {
        if (!loginResult.OrgContextSelected || loginResult.OrganizationApplicationId <= 0)
            return;

        var claimCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claimCode in (loginResult.Claims ?? [])
                     .Where(c => !string.IsNullOrWhiteSpace(c.ClaimCode))
                     .Select(c => c.ClaimCode!.Trim()))
        {
            claimCodes.Add(claimCode);
        }

        var personPositionId = await ResolvePersonPositionIdAsync(loginResult);
        if (!personPositionId.HasValue)
        {
            loginResult.Claims = claimCodes
                .Select(code => new ClaimDto { ClaimCode = code })
                .ToList();
            return;
        }

        var personnel = await _context.Personnels
            .Where(p => p.PersonnelId == personPositionId.Value && p.IsActive)
            .Select(p => new { p.PositionId, p.PositionNominateId })
            .FirstOrDefaultAsync();

        // Si el registro de personal no contiene un position_id, se usa
        // position_nominate_id como cargo de respaldo para resolver los permisos.
        var positionId = personnel?.PositionId ?? personnel?.PositionNominateId;

        if (!positionId.HasValue || positionId.Value == Guid.Empty)
        {
            loginResult.Claims = claimCodes
                .Select(code => new ClaimDto { ClaimCode = code })
                .ToList();
            return;
        }

        var positionRoleClaimCodes = await _context.RoleClaims
            .Where(rc => rc.IsActive
                      && rc.Role.IsActive
                      && rc.Role.OrganizationApplicationId == loginResult.OrganizationApplicationId
                      && rc.Role.PositionId == positionId.Value
                      && rc.Claim.IsActive
                      && rc.Claim.ApplicationId == loginResult.ApplicationId)
            .Select(rc => rc.Claim.Code)
            .Distinct()
            .ToListAsync();

        foreach (var claimCode in positionRoleClaimCodes.Where(code => !string.IsNullOrWhiteSpace(code)))
        {
            claimCodes.Add(claimCode.Trim());
        }

        loginResult.Claims = claimCodes
            .Select(code => new ClaimDto { ClaimCode = code })
            .ToList();
    }

    private async Task<Guid?> ResolvePersonPositionIdAsync(LoginResultDto loginResult)
    {
        if (loginResult.PersonPositionId.HasValue)
            return loginResult.PersonPositionId;

        loginResult.PersonPositionId = await _context.Users
            .Where(u => u.UserId == loginResult.UserId && u.IsActive)
            .Select(u => u.PersonPositionId)
            .FirstOrDefaultAsync();

        return loginResult.PersonPositionId;
    }

    public async Task<Response<ClaimDto>> GetClaimsAsync(Guid userId, int orgAppId)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            var payload = JsonConvert.SerializeObject(new
            {
                user_id = userId,
                organization_application_id = orgAppId
            }, settings);

            var parameters = new DynamicParameters();
            parameters.Add("payload", payload, DbType.String, ParameterDirection.Input);
            parameters.Add("result", dbType: DbType.String, direction: ParameterDirection.Output, size: int.MaxValue);

            await _connection.ExecuteAsync(
                "read_user_effective_claims",
                parameters,
                commandType: CommandType.StoredProcedure);

            var claimsJson = parameters.Get<string>("result");
            if (string.IsNullOrEmpty(claimsJson))
                return new Response<ClaimDto>("No se encontraron claims para el usuario", false);

            var claimsData = JsonConvert.DeserializeObject<ClaimsWrapper>(claimsJson, settings);
            if (claimsData?.Claims == null || !claimsData.Claims.Any())
                return new Response<ClaimDto>("No se encontraron claims para el usuario", false);

            var claimDto = new ClaimDto
            {
                ClaimCode = string.Join(",", claimsData.Claims.Select(c => c.ClaimCode))
            };

            return new Response<ClaimDto>
            {
                Succeeded = true,
                Data = claimDto
            };
        }
        catch (Exception ex)
        {
            return new Response<ClaimDto>($"Error al obtener los claims del usuario: {ex.Message}", false);
        }
    }

    public async Task<Response<List<AreaDto>>> GetAreasForOrgByAreaIdAsync(Guid areaId)
    {
        return await _userService.GetAreasForOrganizationAsync(areaId);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, Guid applicationId)
    {
        var key = await GetSigningKeyForApp(applicationId);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("application_id", applicationId.ToString()),
            new Claim("purpose", "pwd_reset")
        };
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:PasswordResetTokenExpiryMinutes", 30)),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class OrgContextRow
    {
        public int OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string? Acronym { get; set; }
        public string? RoleName { get; set; }
        public int RoleId { get; set; }
    }

    public async Task<Response<string>> RevokeRefreshTokenAsync(string refreshToken)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
            var tokenHash = Convert.ToHexString(hashBytes).ToLower();

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

            if (storedToken == null)
                return new Response<string>("Refresh token inv\u00e1lido", false);

            if (storedToken.IsRevoked)
                return new Response<string>("El refresh token ya ha sido revocado", false);

            storedToken.IsRevoked = true;

            if (storedToken.SessionId.HasValue)
            {
                var session = await _context.UserSessions.FindAsync(storedToken.SessionId.Value);
                if (session != null && session.IsActive)
                {
                    session.IsActive = false;
                    session.RevokedAt = DateTime.UtcNow;
                    session.RevokedByIp = GetClientIp();
                }
            }

            await _context.SaveChangesAsync();

            await WriteAuditAsync(SecurityAuditEventTypes.TokenRevoked, true, storedToken.UserId, storedToken.ApplicationId,
                JsonConvert.SerializeObject(new { token_id = storedToken.TokenId, session_id = storedToken.SessionId }));

            return new Response<string>("Sesi\u00f3n cerrada exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al revocar el refresh token: {ex.Message}", false);
        }
    }

    public async Task<Response<List<UserSessionDto>>> GetUserSessionsAsync(Guid userId, Guid applicationId, Guid? currentSessionId = null)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.ApplicationId == applicationId && s.IsActive)
                .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
                .Select(s => new UserSessionDto
                {
                    SessionId = s.SessionId,
                    Ip = s.Ip,
                    IsTrusted = s.IsTrusted,
                    IsCurrentSession = currentSessionId.HasValue && s.SessionId == currentSessionId.Value,
                    CreatedAt = s.CreatedAt,
                    LastActivityAt = s.LastActivityAt,
                    AbsoluteExpiresAt = s.AbsoluteExpiresAt
                })
                .ToListAsync();

            return new Response<List<UserSessionDto>>(sessions, "OK");
        }
        catch (Exception ex)
        {
            return new Response<List<UserSessionDto>>($"Error al obtener las sesiones: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> RevokeSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                return new Response<string>("Sesi\u00f3n no encontrada", false);

            if (!session.IsActive)
                return new Response<string>("La sesi\u00f3n ya est\u00e1 cerrada", false);

            session.IsActive = false;
            session.RevokedAt = DateTime.UtcNow;
            session.RevokedByIp = GetClientIp();

            var tokens = await _context.RefreshTokens
                .Where(t => t.SessionId == sessionId && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
                token.IsRevoked = true;

            await _context.SaveChangesAsync();

            await WriteAuditAsync(SecurityAuditEventTypes.TokenRevoked, true, null, session.ApplicationId,
                JsonConvert.SerializeObject(new { session_id = sessionId, reason = "remote_revocation" }));

            return new Response<string>("Sesi\u00f3n cerrada exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al cerrar la sesi\u00f3n: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> RevokeAllSessionsByApplicationIdAsync(Guid applicationId)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Where(s => s.ApplicationId == applicationId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.RevokedAt = DateTime.UtcNow;
                session.RevokedByIp = GetClientIp();
            }

            var sessionIds = sessions.Select(s => s.SessionId).ToList();
            var tokens = await _context.RefreshTokens
                .Where(t => t.SessionId != null && sessionIds.Contains(t.SessionId.Value) && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
                token.IsRevoked = true;

            await _context.SaveChangesAsync();

            await WriteAuditAsync(SecurityAuditEventTypes.TokenRevoked, true, null, applicationId,
                JsonConvert.SerializeObject(new { reason = "revoke_all_sessions_by_app", session_count = sessions.Count }));

            return new Response<string>($"{sessions.Count} sesi\u00f3n(es) cerradas exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al cerrar las sesiones: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> RevokeAllUserSessionsAsync(Guid userId, Guid applicationId)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.ApplicationId == applicationId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.RevokedAt = DateTime.UtcNow;
                session.RevokedByIp = GetClientIp();
            }

            var sessionIds = sessions.Select(s => s.SessionId).ToList();
            var tokens = await _context.RefreshTokens
                .Where(t => t.SessionId != null && sessionIds.Contains(t.SessionId.Value) && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
                token.IsRevoked = true;

            await _context.SaveChangesAsync();

            await WriteAuditAsync(SecurityAuditEventTypes.TokenRevoked, true, userId, applicationId,
                JsonConvert.SerializeObject(new { reason = "revoke_all_user_sessions", session_count = sessions.Count }));

            return new Response<string>($"{sessions.Count} sesi\u00f3n(es) cerradas exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al cerrar las sesiones del usuario: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> RevokeAllSessionsAsync()
    {
        try
        {
            var sessions = await _context.UserSessions
                .Where(s => s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.RevokedAt = DateTime.UtcNow;
                session.RevokedByIp = GetClientIp();
            }

            var sessionIds = sessions.Select(s => s.SessionId).ToList();
            var tokens = await _context.RefreshTokens
                .Where(t => t.SessionId != null && sessionIds.Contains(t.SessionId.Value) && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
                token.IsRevoked = true;

            await _context.SaveChangesAsync();

            await WriteAuditAsync(SecurityAuditEventTypes.TokenRevoked, true, null, null,
                JsonConvert.SerializeObject(new { reason = "revoke_all_sessions_global", session_count = sessions.Count }));

            return new Response<string>($"{sessions.Count} sesi\u00f3n(es) cerradas exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al cerrar todas las sesiones: {ex.Message}", false);
        }
    }

    public async Task<bool> IsSessionActiveAsync(Guid sessionId)
    {
        var session = await _context.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        return session != null
            && session.IsActive
            && session.AbsoluteExpiresAt >= DateTime.UtcNow;
    }

    private string GetCorrelationId()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null) return string.Empty;
        if (ctx.Items.TryGetValue("X-Correlation-Id", out var id) && id is string s)
            return s;
        return ctx.Request.Headers.TryGetValue("X-Correlation-Id", out var h) ? h.ToString() : string.Empty;
    }

    private string GetClientIp()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null) return string.Empty;

        if (ctx.Request.Headers.TryGetValue("X-Forwarded-For", out var xff) && !string.IsNullOrWhiteSpace(xff))
        {
            var candidates = xff.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var candidate in candidates)
            {
                var parsed = ParseIpStrippingPort(candidate);
                if (parsed != null &&
                    !System.Net.IPAddress.IsLoopback(parsed) &&
                    !IsPrivateIp(parsed))
                    return parsed.ToString();
            }

            var first = candidates.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                var parsed = ParseIpStrippingPort(first);
                if (parsed != null) return parsed.ToString();
            }
        }

        if (ctx.Request.Headers.TryGetValue("X-Real-IP", out var xRealIp) && !string.IsNullOrWhiteSpace(xRealIp))
        {
            var parsed = ParseIpStrippingPort(xRealIp.ToString().Trim());
            if (parsed != null) return parsed.ToString();
        }

        var remoteIp = ctx.Connection.RemoteIpAddress;
        if (remoteIp == null) return string.Empty;

        if (remoteIp.IsIPv4MappedToIPv6)
            remoteIp = remoteIp.MapToIPv4();
        else if (remoteIp.Equals(System.Net.IPAddress.IPv6Loopback))
            remoteIp = System.Net.IPAddress.Loopback;

        return remoteIp.ToString();
    }

    /// <summary>
    /// Parsea una cadena IP removiendo el puerto si lo tuviera.
    /// Soporta: IPv4 (1.2.3.4), IPv4:puerto (1.2.3.4:5678),
    /// IPv6 puro (::1), IPv6 con bracket ([::1]:5678).
    /// </summary>
    private static System.Net.IPAddress? ParseIpStrippingPort(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        raw = raw.Trim();

        // IPv6 con bracket: [::1]:5678  →  extraer lo que está entre [ ]
        if (raw.StartsWith('['))
        {
            var closing = raw.IndexOf(']');
            if (closing > 1)
                raw = raw[1..closing];
        }
        else
        {
            // IPv4 con puerto: 1.2.3.4:5678  →  solo un ':' significa que hay puerto
            // IPv6 puro tiene múltiples ':' por lo que no se recorta
            var colonCount = raw.Count(c => c == ':');
            if (colonCount == 1)
                raw = raw[..raw.IndexOf(':')];
        }

        if (System.Net.IPAddress.TryParse(raw, out var ip))
        {
            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();
            return ip;
        }

        return null;
    }

    private static bool IsPrivateIp(System.Net.IPAddress ip)
    {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            return false;

        var bytes = ip.GetAddressBytes();
        return bytes[0] == 10 ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168);
    }

    private string GetUserAgent()
    {
        var ctx = _httpContextAccessor.HttpContext;
        return ctx?.Request.Headers.UserAgent.ToString() ?? string.Empty;
    }

    private async Task WriteAuditAsync(string eventType, bool success, Guid? userId = null, Guid? applicationId = null, string? details = null)
    {
        try
        {
            _context.SecurityAuditLogs.Add(new SecurityAuditLog
            {
                EventType = eventType,
                UserId = userId,
                ApplicationId = applicationId,
                CorrelationId = GetCorrelationId(),
                Ip = GetClientIp(),
                UserAgent = GetUserAgent(),
                Details = details,
                Success = success,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing security audit log for event {EventType}", eventType);
        }
    }

    public Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, string email)
    {
        var key = GetSigningKeyForUser(userId);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("email", email),
            new Claim("purpose", "email_confirm")
        };
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_config.GetValue<int>("Jwt:EmailConfirmationTokenExpiryHours", 24)),
            signingCredentials: creds);
        return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
    }

    public Task<Response<Guid>> ValidateEmailConfirmationTokenAsync(string token)
    {
        try
        {
            // Lectura sin validación para obtener el userId y derivar la clave
            var jwt = SafeReadToken(token);
            var userIdClaim = jwt?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return Task.FromResult(new Response<Guid>("Token de confirmación inválido", false));

            var key = GetSigningKeyForUser(userId);
            ClaimsPrincipal principal;
            try
            {
                principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _config["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);
            }
            catch (SecurityTokenExpiredException)
            {
                return Task.FromResult(new Response<Guid>("El enlace de confirmación ha expirado", false));
            }
            catch
            {
                return Task.FromResult(new Response<Guid>("Token de confirmación inválido", false));
            }

            var purpose = principal.Claims.FirstOrDefault(c => c.Type == "purpose")?.Value;
            if (!string.Equals(purpose, "email_confirm", StringComparison.Ordinal))
                return Task.FromResult(new Response<Guid>("Token inválido para confirmación de email", false));

            return Task.FromResult(new Response<Guid>(userId, "OK"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new Response<Guid>($"Error al validar el token de confirmación: {ex.Message}", false));
        }
    }

    private SymmetricSecurityKey GetSigningKeyForUser(Guid userId)
    {
        var masterKey = _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(masterKey))
            throw new InvalidOperationException("La clave maestra Jwt:Key no está configurada.");

        var issuer = _config["Jwt:Issuer"] ?? string.Empty;
        var audience = _config["Jwt:Audience"] ?? string.Empty;
        var context = $"email_confirm|{issuer}|{audience}|user:{userId}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(masterKey));
        var keyBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(context));
        return new SymmetricSecurityKey(keyBytes);
    }

    private async Task<SymmetricSecurityKey> GetSigningKeyForApp(Guid appId)
    {
        var keyBytes = await DeriveAppKeyBytes(appId);
        return new SymmetricSecurityKey(keyBytes);
    }

    private sealed class RandomStringResponse
    {
        [JsonProperty("random_string")]
        public string? RandomString { get; set; }
    }

    private async Task<byte[]> DeriveAppKeyBytes(Guid appId)
    {
        var json = await _connection.QueryFirstOrDefaultAsync<string>(
            "read_random_string",
            new { application_id = appId },
            commandType: CommandType.StoredProcedure);

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("No se pudo obtener el string Key");

        var parsed = JsonConvert.DeserializeObject<RandomStringResponse>(json);
        var str = parsed?.RandomString;

        if (string.IsNullOrWhiteSpace(str))
            throw new InvalidOperationException("El resultado 'random_string' est\u00e1 vac\u00edo.");

        var masterKey = _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(masterKey))
            throw new InvalidOperationException("La clave maestra Jwt:Key no est\u00e1 configurada.");

        var issuer = _config["Jwt:Issuer"] ?? string.Empty;
        var audience = _config["Jwt:Audience"] ?? string.Empty;
        var context = $"{issuer}|{audience}|app:{appId}|str:{str}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(masterKey));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(context));
    }

    private static JwtSecurityToken? SafeReadToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadJwtToken(token);
        }
        catch
        {
            return null;
        }
    }

    private static Guid? TryGetApplicationId(JwtSecurityToken? jwt)
    {
        if (jwt == null) return null;
        var appIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "application_id")?.Value;
        return Guid.TryParse(appIdClaim, out var v) ? v : null;
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    public async Task<Response<string>> GetAppSecretAsync(Guid appId)
    {
        try
        {
            var bytes = await DeriveAppKeyBytes(appId);
            var base64Url = ToBase64Url(bytes);
            return new Response<string> { Succeeded = true, Data = base64Url };
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al obtener el secreto del aplicativo: {ex.Message}", false);
        }
    }

    public async Task<Response<List<UserOrgAppDto>>> GetUserOrgAppsAsync(Guid userId, Guid applicationId, string? clientIp = null, string? userAgent = null)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            var filterJson = JsonConvert.SerializeObject(new
            {
                user_id = userId,
                application_id = applicationId,
                is_active = true
            }, settings);

            var user = await _context.Users.FindAsync(userId);
            var isExternal = user?.IsExternal ?? false;

            List<UserOrgAppDto> userOrgApps;

            if (isExternal)
            {
                userOrgApps = await _context.OrganizationApplications
                    .Where(oa =>
                        oa.ApplicationId == applicationId &&
                        oa.Application.IsPublic &&
                        oa.Application.IsActive &&
                        oa.IsActive)
                    .Select(oa => new UserOrgAppDto
                    {
                        OrganizationApplicationId = oa.OrganizationApplicationId,
                        OrganizationId = oa.OrganizationId,
                        OrganizationName = oa.Organization.OrganizationName ?? string.Empty,
                        ApplicationId = oa.ApplicationId,
                        ApplicationName = oa.Application.ApplicationName
                    })
                    .ToListAsync();
            }
            else
            {
                var json = await _connection.QueryFirstOrDefaultAsync<string>(
                    "read_user_org_apps",
                    new { page_number = 1, page_size = 100, filter = filterJson },
                    commandType: CommandType.StoredProcedure);

                if (string.IsNullOrEmpty(json) || json == "{}")
                    return new Response<List<UserOrgAppDto>>("No se encontraron aplicaciones vinculadas", false);

                var wrapper = JsonConvert.DeserializeObject<ResultWrapper<UserOrgAppDto>>(json);
                userOrgApps = wrapper?.Data ?? new List<UserOrgAppDto>();
            }

            // Evaluar políticas por cada org-app si se cuenta con IP
            if (!string.IsNullOrWhiteSpace(clientIp))
            {
                var userAppMap = await _context.UserApplications
                    .Where(ua => ua.UserId == userId &&
                                 userOrgApps.Select(x => x.OrganizationApplicationId).Contains(ua.OrganizationApplicationId) &&
                                 ua.IsActive)
                    .Select(ua => new { ua.OrganizationApplicationId, ua.UserApplicationId })
                    .ToListAsync();

                foreach (var orgApp in userOrgApps)
                {
                    var match = userAppMap.FirstOrDefault(m => m.OrganizationApplicationId == orgApp.OrganizationApplicationId);
                    if (match == null)
                    {
                        orgApp.PolicyAccess = true;
                        continue;
                    }

                    var evalResult = await _policyEvaluator.EvaluateAsync(match.UserApplicationId, clientIp, userAgent);
                    if (!evalResult.Succeeded)
                    {
                        orgApp.PolicyAccess = false;
                        orgApp.PolicyMessage = $"Error al evaluar políticas: {evalResult.Message}";
                    }
                    else
                    {
                        orgApp.PolicyAccess = evalResult.Data.IsAllowed;
                        orgApp.PolicyMessage = evalResult.Data.IsAllowed ? null : evalResult.Data.Reason;
                    }
                }
            }

            return new Response<List<UserOrgAppDto>>(userOrgApps, "OK");
        }
        catch (Exception ex)
        {
            return new Response<List<UserOrgAppDto>>($"Error al obtener org-apps del usuario: {ex.Message}", false);
        }
    }

    public async Task<PagedResponse<SessionAdminDto>> GetAllSessionsAsync(int? pageNumber, int? pageSize, SessionFilterDto? filter)
    {
        if (pageNumber == null || pageNumber <= 0)
            pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100)
            pageSize = 50;
        try
        {
            var now = DateTime.UtcNow;
            // Base query sin Include; el Include se aplica solo sobre la página final
            var query = _context.UserSessions
                .AsNoTracking()
                .Where(s => s.AbsoluteExpiresAt > now)
                .AsQueryable();

            if (filter != null)
            {
                if (filter.IsActive.HasValue)
                    query = query.Where(s => s.IsActive == filter.IsActive.Value);

                if (filter.ApplicationId.HasValue)
                    query = query.Where(s => s.ApplicationId == filter.ApplicationId.Value);

                if (filter.UserId.HasValue)
                    query = query.Where(s => s.UserId == filter.UserId.Value);
            }

            bool hasPersonFilter = !string.IsNullOrWhiteSpace(filter?.FirstName)
                || !string.IsNullOrWhiteSpace(filter?.LastName)
                || !string.IsNullOrWhiteSpace(filter?.IdentificationNumber);

            Dictionary<Guid, IdentityPersonResponseDTO> personMap = [];

            if (hasPersonFilter)
            {
                // Obtener los PersonnelIds que ya están en el scope de la query actual
                var scopedUserIds = await query
                    .Where(s => s.UserId.HasValue)
                    .Select(s => s.UserId!.Value)
                    .Distinct()
                    .ToListAsync();

                if (scopedUserIds.Count == 0)
                    return new PagedResponse<SessionAdminDto>([], pageNumber.Value, pageSize.Value, 0, 0);

                var scopedPersonnelIds = await _context.Users
                    .Where(u => scopedUserIds.Contains(u.UserId))
                    .Select(u => u.PersonnelId)
                    .Distinct()
                    .ToListAsync();

                if (scopedPersonnelIds.Count == 0)
                    return new PagedResponse<SessionAdminDto>([], pageNumber.Value, pageSize.Value, 0, 0);

                List<IdentityPersonResponseDTO> matchingPersons;

                if (!string.IsNullOrWhiteSpace(filter?.IdentificationNumber))
                {
                    // Búsqueda exacta por cédula, validada dentro del scope
                    var byIdResponse = await _personService.GetPersonByCedulaAsync(filter.IdentificationNumber, false);
                    matchingPersons = byIdResponse.Succeeded && byIdResponse.Data != null
                                      && scopedPersonnelIds.Contains(byIdResponse.Data.Id)
                        ? [byIdResponse.Data]
                        : [];
                }
                else
                {
                    // Búsqueda por nombre acotada a los PersonnelIds del scope (igual que ListUsersByOrganizationAsync)
                    var searchTerm = $"{filter?.FirstName} {filter?.LastName}".Trim();
                    var searchResponse = await _personService.SearchPersonsAsync(
                        pageNumber: 1,
                        pageSize: scopedPersonnelIds.Count,
                        searchTerm: searchTerm,
                        ids: scopedPersonnelIds);

                    matchingPersons = searchResponse.Succeeded && searchResponse.Data != null
                        ? searchResponse.Data
                        : [];
                }

                if (matchingPersons.Count == 0)
                    return new PagedResponse<SessionAdminDto>([], pageNumber.Value, pageSize.Value, 0, 0);

                // Construir el mapa ahora y reutilizarlo al final
                personMap = matchingPersons
                    .GroupBy(p => p.Id)
                    .ToDictionary(g => g.Key, g => g.First());

                var matchingPersonnelIds = matchingPersons.Select(p => p.Id).ToList();

                var matchingUserIds = await _context.Users
                    .Where(u => matchingPersonnelIds.Contains(u.PersonnelId))
                    .Select(u => u.UserId)
                    .ToListAsync();

                if (matchingUserIds.Count == 0)
                    return new PagedResponse<SessionAdminDto>([], pageNumber.Value, pageSize.Value, 0, 0);

                query = query.Where(s => s.UserId.HasValue && matchingUserIds.Contains(s.UserId.Value));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Include solo sobre la página final
            var sessions = await query
                .Include(s => s.User)
                .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToListAsync();

            if (!hasPersonFilter)
            {
                // Sin filtro de persona: bulk fetch en una sola llamada (igual que ListUsersByOrganizationAsync)
                var personnelIds = sessions
                    .Where(s => s.User != null && s.User.PersonnelId != Guid.Empty)
                    .Select(s => s.User!.PersonnelId)
                    .Distinct();

                var bulkResult = await _personService.GetPersonsByIdsAsync(personnelIds);
                if (bulkResult.Succeeded && bulkResult.Data != null)
                    personMap = bulkResult.Data.ToDictionary(p => p.Id);
            }
            // Con filtro de persona: personMap ya está construido desde la búsqueda anterior

            var result = sessions.Select(s =>
            {
                var personnelId = s.User?.PersonnelId ?? Guid.Empty;
                personMap.TryGetValue(personnelId, out var person);
                return new SessionAdminDto
                {
                    SessionId = s.SessionId,
                    UserId = s.UserId,
                    Email = s.User?.Email,
                    FirstName = person?.FirstName,
                    LastName = person?.LastName,
                    IdentificationNumber = person?.IdentificationNumber,
                    ApplicationId = s.ApplicationId,
                    Ip = s.Ip,
                    UserAgent = s.UserAgent,
                    IsTrusted = s.IsTrusted,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    LastActivityAt = s.LastActivityAt,
                    AbsoluteExpiresAt = s.AbsoluteExpiresAt,
                    RevokedAt = s.RevokedAt
                };
            }).ToList();

            return new PagedResponse<SessionAdminDto>(result, pageNumber.Value, pageSize.Value, totalCount, totalPages);
        }
        catch (Exception ex)
        {
            return new PagedResponse<SessionAdminDto>([], $"Error al obtener las sesiones: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // M2M — Machine-to-Machine (Client Credentials Grant)
    // ══════════════════════════════════════════════════════════════════════

    public async Task<Response<AppTokenResponseDto>> AppLoginAsync(AppLoginRequestDto dto)
    {
        var ip = GetClientIp();
        try
        {
            // 1. Rate limiting: máx 5 fallos por IP en los últimos 15 minutos
            var since = DateTime.UtcNow.AddMinutes(-15);
            var recentFailures = await _context.SecurityAuditLogs
                .AsNoTracking()
                .CountAsync(l =>
                    l.Ip == ip &&
                    l.EventType == SecurityAuditEventTypes.AppLoginFailure &&
                    !l.Success &&
                    l.CreatedAt >= since);

            if (recentFailures >= 5)
            {
                await WriteAuditAsync(SecurityAuditEventTypes.AppLoginFailure, false, null, dto.ClientId,
                    System.Text.Json.JsonSerializer.Serialize(new { reason = "rate_limit_exceeded", ip }));
                return new Response<AppTokenResponseDto>("Demasiados intentos fallidos. Intenta de nuevo en 15 minutos.", false);
            }

            // 2. Validar que la aplicación exista y esté activa
            var appCredential = await _context.ApplicationCredentials
                .FirstOrDefaultAsync(a => a.AppCredentialId == dto.ClientId && a.IsActive);

            if (appCredential == null)
            {
                await WriteAuditAsync(SecurityAuditEventTypes.AppLoginFailure, false, null, dto.ClientId,
                    System.Text.Json.JsonSerializer.Serialize(new { reason = "credential_not_found" }));
                return new Response<AppTokenResponseDto>("Credenciales inválidas", false);
            }

            if(appCredential.ExpiresAt != null && appCredential.ExpiresAt <= DateTime.UtcNow)
            {
                await WriteAuditAsync(SecurityAuditEventTypes.AppLoginFailure, false, null, dto.ClientId,
                    System.Text.Json.JsonSerializer.Serialize(new { reason = "credential_expired" }));
                return new Response<AppTokenResponseDto>("Credenciales inválidas", false);
            }

            // 3. Obtener ApplicationRole vinculado directamente a esta credencial por AppCredentialId
            var applicationRole = await _context.ApplicationRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(ar =>
                    ar.AppCredentialId == dto.ClientId &&
                    ar.IsActive);

            if (applicationRole == null)
            {
                await WriteAuditAsync(SecurityAuditEventTypes.AppLoginFailure, false, null, dto.ClientId,
                    System.Text.Json.JsonSerializer.Serialize(new { reason = "application_role_not_found" }));
                return new Response<AppTokenResponseDto>("Credenciales inválidas", false);
            }

            var organizationApplicationId = applicationRole.OrganizationApplicationId;

            // 4. Validar que el OrganizationApplication exista y esté asociado a esta app
            var orgApp = await _context.OrganizationApplications
                .Include(oa => oa.Organization)
                .FirstOrDefaultAsync(oa =>
                    oa.OrganizationApplicationId == organizationApplicationId &&
                    oa.ApplicationId == appCredential.ApplicationId &&
                    oa.IsActive);

            if (orgApp == null)
            {
                await WriteAuditAsync(SecurityAuditEventTypes.AppLoginFailure, false, null, dto.ClientId,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        reason = "org_app_not_found",
                        organization_application_id = organizationApplicationId
                    }));
                return new Response<AppTokenResponseDto>("Credenciales inválidas", false);
            }

            if (!SecretHasher.Verify(dto.ClientSecret, appCredential.ClientSecretHash))
            {
                await WriteAuditAsync(SecurityAuditEventTypes.AppLoginFailure, false, null, dto.ClientId,
                    System.Text.Json.JsonSerializer.Serialize(new { reason = "invalid_secret" }));
                return new Response<AppTokenResponseDto>("Credenciales inválidas", false);
            }

            // 6. Cargar permisos: ApplicationRoleClaim → Claim del rol vinculado a esta credencial
            var permissions = await _context.ApplicationRoleClaims
                .AsNoTracking()
                .Where(rc => rc.ApplicationRoleId == applicationRole.ApplicationRoleId && rc.IsActive)
                .Join(_context.Claims.Where(c => c.IsActive),
                    rc => rc.ClaimId,
                    c => c.ClaimId,
                    (rc, c) => c.Code)
                .Distinct()
                .ToListAsync();

            // 7. Generar JWT M2M
            var accessToken = await GenerateAppJwtTokenAsync(
                appCredentialId: dto.ClientId,
                applicationId: appCredential.ApplicationId,
                orgApp: orgApp,
                permissions: permissions);

            // 8. Crear sesión M2M
            var sessionId = Guid.NewGuid();
            var absoluteExpiryDays = _config.GetValue<int>("M2M:SessionAbsoluteExpiryDays", 30);
            var session = new UserSession
            {
                SessionId = sessionId,
                UserId = null,
                ApplicationId = appCredential.ApplicationId,
                AppCredentialId = dto.ClientId,
                Ip = ip,
                UserAgent = GetUserAgent(),
                IsTrusted = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                AbsoluteExpiresAt = DateTime.UtcNow.AddDays(absoluteExpiryDays)
            };
            _context.UserSessions.Add(session);

            // 9. Crear refresh token M2M
            var rawTokenBytes = RandomNumberGenerator.GetBytes(32);
            var rawRefreshToken = Convert.ToBase64String(rawTokenBytes)
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            using var sha256 = SHA256.Create();
            var tokenHash = Convert.ToHexString(
                sha256.ComputeHash(Encoding.UTF8.GetBytes(rawRefreshToken))).ToLower();

            var refreshExpiryDays = _config.GetValue<int>("M2M:RefreshTokenExpiryDays", 30);
            var refreshToken = new RefreshToken
            {
                TokenId = Guid.NewGuid(),
                TokenHash = tokenHash,
                UserId = null,
                ApplicationId = appCredential.ApplicationId,
                AppCredentialId = dto.ClientId,
                AuthData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    application_id = appCredential.ApplicationId,
                    organization_application_id = organizationApplicationId
                }),
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays),
                CreatedAt = DateTime.UtcNow,
                CorrelationId = GetCorrelationId(),
                SessionId = sessionId
            };
            _context.RefreshTokens.Add(refreshToken);

            await _context.SaveChangesAsync();

            // 10. Audit log + actualizar last_used_at (fire-and-forget)
            await WriteAuditAsync(SecurityAuditEventTypes.AppLoginSuccess, true, null, dto.ClientId,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    credential_id = appCredential.AppCredentialId,
                    session_id = sessionId,
                    app_id = appCredential.ApplicationId
                }));

            _ = Task.Run(async () =>
            {
                try
                {
                    appCredential.LastUsedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                catch { /* no bloquear el flujo principal */ }
            });

            return new Response<AppTokenResponseDto>
            {
                Succeeded = true,
                Message = "Autenticación M2M exitosa",
                Data = new AppTokenResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = rawRefreshToken,
                    TokenType = "Bearer",
                    ExpiresIn = 3600
                }
            };
        }
        catch (Exception ex)
        {
            return new Response<AppTokenResponseDto>($"Error en autenticación M2M: {ex.Message}", false);
        }
    }

    public async Task<Response<AppTokenResponseDto>> RefreshAppTokenAsync(string refreshToken)
    {
        try
        {
            using var sha256 = SHA256.Create();
            var tokenHash = Convert.ToHexString(
                sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken))).ToLower();

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.AppCredentialId != null);

            if (storedToken == null)
                return new Response<AppTokenResponseDto>("Refresh token inválido", false);

            if (storedToken.IsRevoked)
                return new Response<AppTokenResponseDto>("El refresh token ha sido revocado", false);

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return new Response<AppTokenResponseDto>("El refresh token ha expirado", false);

            if (storedToken.SessionId.HasValue)
            {
                var session = await _context.UserSessions.FindAsync(storedToken.SessionId.Value);
                if (session != null)
                {
                    if (!session.IsActive)
                        return new Response<AppTokenResponseDto>("La sesión M2M ha sido revocada.", false);
                    if (session.AbsoluteExpiresAt < DateTime.UtcNow)
                    {
                        session.IsActive = false;
                        await _context.SaveChangesAsync();
                        return new Response<AppTokenResponseDto>("La sesión M2M ha expirado. Vuelve a autenticarte.", false);
                    }
                    session.LastActivityAt = DateTime.UtcNow;
                }
            }

            // Recuperar contexto del token
            var authData = System.Text.Json.JsonSerializer.Deserialize<AppAuthData>(storedToken.AuthData ?? "{}");
            if (authData == null)
                return new Response<AppTokenResponseDto>("Error al recuperar el contexto de autenticación", false);

            // Validar que la credencial sigue activa y no ha expirado
            var credential = await _context.ApplicationCredentials
                .FirstOrDefaultAsync(c => c.AppCredentialId == storedToken.AppCredentialId);

            if (credential == null || !credential.IsActive || credential.ExpiresAt <= DateTime.UtcNow)
                return new Response<AppTokenResponseDto>("La credencial M2M ha sido revocada o expiró. Genera una nueva.", false);

            // Cargar orgApp para el JWT
            var orgApp = await _context.OrganizationApplications
                .Include(oa => oa.Organization)
                .FirstOrDefaultAsync(oa =>
                    oa.OrganizationApplicationId == authData.OrganizationApplicationId &&
                    oa.IsActive);

            if (orgApp == null)
                return new Response<AppTokenResponseDto>("El contexto de organización ya no está disponible", false);

            // Recargar permisos frescos del rol vinculado a esta credencial
            var appRole = await _context.ApplicationRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(ar =>
                    ar.AppCredentialId == storedToken.AppCredentialId &&
                    ar.IsActive);

            var permissions = appRole == null
                ? []
                : await _context.ApplicationRoleClaims
                    .AsNoTracking()
                    .Where(rc => rc.ApplicationRoleId == appRole.ApplicationRoleId && rc.IsActive)
                    .Join(_context.Claims.Where(c => c.IsActive),
                        rc => rc.ClaimId,
                        c => c.ClaimId,
                        (rc, c) => c.Code)
                    .Distinct()
                    .ToListAsync();

            var newAccessToken = await GenerateAppJwtTokenAsync(storedToken.AppCredential!.AppCredentialId, authData.ApplicationId, orgApp, permissions);

            // Nuevo refresh token
            var rawNewBytes = RandomNumberGenerator.GetBytes(32);
            var rawNewToken = Convert.ToBase64String(rawNewBytes)
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            using var sha256New = SHA256.Create();
            var newHash = Convert.ToHexString(
                sha256New.ComputeHash(Encoding.UTF8.GetBytes(rawNewToken))).ToLower();

            var refreshExpiryDays = _config.GetValue<int>("M2M:RefreshTokenExpiryDays", 30);
            var newRefreshToken = new RefreshToken
            {
                TokenId = Guid.NewGuid(),
                TokenHash = newHash,
                UserId = null,
                ApplicationId = authData.ApplicationId,
                AppCredentialId = storedToken.AppCredentialId,
                AuthData = storedToken.AuthData,
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays),
                CreatedAt = DateTime.UtcNow,
                CorrelationId = GetCorrelationId(),
                SessionId = storedToken.SessionId
            };

            _context.RefreshTokens.Add(newRefreshToken);
            storedToken.IsRevoked = true;
            storedToken.ReplacedBy = newRefreshToken.TokenId;

            await _context.SaveChangesAsync();

            await WriteAuditAsync(SecurityAuditEventTypes.AppTokenRefreshed, true, null, authData.ApplicationId,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    old_token_id = storedToken.TokenId,
                    new_token_id = newRefreshToken.TokenId
                }));

            return new Response<AppTokenResponseDto>
            {
                Succeeded = true,
                Message = "Tokens M2M refrescados",
                Data = new AppTokenResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = rawNewToken,
                    TokenType = "Bearer",
                    ExpiresIn = 3600
                }
            };
        }
        catch (Exception ex)
        {
            return new Response<AppTokenResponseDto>($"Error al refrescar tokens M2M: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> VerifyAppTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtIssuer = _config["Jwt:Issuer"];
            var jwtAudience = _config["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtIssuer))
                return new Response<string>("El emisor JWT no está configurado.", false);
            if (string.IsNullOrWhiteSpace(jwtAudience))
                return new Response<string>("La audiencia JWT no está configurada.", false);

            var readToken = SafeReadToken(token);
            var appId = TryGetApplicationId(readToken);
            if (appId == null)
                return new Response<string>("El token no es valido", false);

            var isServiceAccount = readToken?.Claims
                .FirstOrDefault(c => c.Type == "is_service_account")?.Value;
            if (!string.Equals(isServiceAccount, "true", StringComparison.OrdinalIgnoreCase))
                return new Response<string>("El token no corresponde a una cuenta de servicio.", false);

            var signingKey = await GetSigningKeyForApp(appId.Value);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken _);

                return new Response<string> { Succeeded = true, Message = "Token de aplicación válido" };
            }
            catch (SecurityTokenExpiredException)
            {
                return new Response<string>("El token ha expirado.", false);
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                return new Response<string>("La firma del token no es válida.", false);
            }
            catch (SecurityTokenException ex)
            {
                return new Response<string>($"Token inválido: {ex.Message}", false);
            }
            catch (Exception ex)
            {
                return new Response<string>($"Error al validar el token: {ex.Message}", false);
            }
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error inesperado al verificar el token de aplicación: {ex.Message}", false);
        }
    }

    private async Task<string> GenerateAppJwtTokenAsync(
        Guid appCredentialId,
        Guid applicationId,
        OrganizationApplication orgApp,
        List<string> permissions)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, appCredentialId.ToString()),
            new Claim("application_id", applicationId.ToString()),
            new Claim("organization_application_id", orgApp.OrganizationApplicationId.ToString()),
            new Claim("organization_id", orgApp.OrganizationId.ToString()),
            new Claim("is_service_account", "true"),
            new Claim("gobid_logo_url", _config["GOBIDLogoUrl"] ?? string.Empty)
        };

        if (permissions.Count > 0)
        {
            var permissionsJson = System.Text.Json.JsonSerializer.Serialize(permissions.ToArray());
            claims.Add(new Claim("permissions", permissionsJson));
        }

        var signingKey = await GetSigningKeyForApp(applicationId);
        signingKey.KeyId = $"app:{applicationId}";

        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_config.GetValue<int>("Jwt:AccessTokenExpiryHours", 1)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Clase auxiliar para deserializar el AuthData de tokens M2M.</summary>
    private sealed class AppAuthData
    {
        [System.Text.Json.Serialization.JsonPropertyName("application_id")]
        public Guid ApplicationId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("organization_application_id")]
        public int OrganizationApplicationId { get; set; }
    }

}
