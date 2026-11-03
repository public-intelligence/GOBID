using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Auth;
using GOBID.Application.DTOs.ApplicationCredential;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using GOBID.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : OwnBaseController
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    public AuthController(IAuthService jwtService, IAuthService authService, IUserService userService, IEmailService emailService) : base(authService)
    {
        _authService = jwtService;
        _userService = userService;
        _emailService = emailService;
    }

    [AllowAnonymous]
    [HttpPost()]
    public async Task<ActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var xff) && !string.IsNullOrWhiteSpace(xff))
        {
            var firstIp = xff.ToString().Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(firstIp))
                ip = firstIp;
        }
        dto.Ip = string.IsNullOrWhiteSpace(ip) ? "unknown" : ip;

        var headersDict = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        dto.Headers = JsonSerializer.Serialize(headersDict);

        var response = await _authService.LoginJwtAsync(dto);
        if (response == null || response.Data == null)
            return Unauthorized(response);

        var orgAppsResult = await _authService.GetUserOrgAppsAsync(
            response.Data.UserId,
            response.Data.ApplicationId,
            dto.Ip,
            Request.Headers.TryGetValue("User-Agent", out var ua) ? ua.ToString() : null);
        if (!orgAppsResult.Succeeded)
            return BadRequest(orgAppsResult);

        var userOrgApps = orgAppsResult.Data ?? [];

        var codeResult = await _authService.GenerateAuthCodeAsync(response.Data);
        if (!codeResult.Succeeded)
            return BadRequest(codeResult);

        string? passwordResetToken = null;
        if (response.Data.MustChangePassword)
            passwordResetToken = await _authService.GeneratePasswordResetTokenAsync(response.Data.UserId, response.Data.ApplicationId);

        return Ok(new
        {
            code = codeResult.Data,
            mustChangePassword = response.Data.MustChangePassword,
            passwordResetToken,
            userOrgApps
        });
    }

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<ActionResult> ExchangeCode([FromBody] ExchangeCodeDto dto)
    {
        var response = await _authService.ExchangeCodeForTokensAsync(dto);
        if (response == null || !response.Succeeded)
            return StatusCode(response?.StatusCode ?? 401, response);

        SetRefreshTokenCookie(response.Data.RefreshToken);

        return Ok(new
        {
            accessToken = response.Data.AccessToken,
            mustChangePassword = response.Data.MustChangePassword
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized("Refresh token no encontrado.");

        var response = await _authService.RefreshAccessTokenAsync(refreshToken);
        if (response == null || !response.Succeeded)
            return StatusCode(response?.StatusCode ?? 401, response);

        SetRefreshTokenCookie(response.Data.RefreshToken);

        return Ok(new
        {
            accessToken = response.Data.AccessToken,
            mustChangePassword = response.Data.MustChangePassword
        });
    }

    // ── M2M: autenticación de aplicaciones (Client Credentials Grant) ─────────

    /// <summary>
    /// Autentica una aplicación como service account usando client_id + client_secret.
    /// Devuelve accessToken y refreshToken directamente en el body (flujo server-to-server).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("app/token")]
    public async Task<ActionResult> AppLogin([FromBody] AppLoginRequestDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var xff) && !string.IsNullOrWhiteSpace(xff))
        {
            var firstIp = xff.ToString().Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(firstIp)) ip = firstIp;
        }

        var response = await _authService.AppLoginAsync(dto);
        if (!response.Succeeded)
            return Unauthorized(response);

        return Ok(response.Data);
    }

    /// <summary>
    /// Refresca los tokens de una sesión M2M. Aplica rotación de refresh token.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("app/refresh")]
    public async Task<ActionResult> AppRefreshToken([FromBody] AppRefreshRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return BadRequest("refresh_token es requerido");

        var response = await _authService.RefreshAppTokenAsync(dto.RefreshToken);
        if (!response.Succeeded)
            return Unauthorized(response);

        return Ok(response.Data);
    }

    /// <summary>
    /// Valida un token JWT emitido por el flujo M2M (Client Credentials Grant).
    /// Verifica firma, vigencia y que pertenezca a una cuenta de servicio.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("app/validate")]
    public async Task<ActionResult> VerifyAppToken([FromQuery] VerifyTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
            return BadRequest("El token es requerido.");

        var response = await _authService.VerifyAppTokenAsync(dto.Token);
        if (!response.Succeeded)
            return Unauthorized(response);

        return Ok(response);
    }

    // ── Logout de usuario ─────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.RevokeRefreshTokenAsync(refreshToken);
            Response.Cookies.Delete("refreshToken");
        }
        return Ok(new { message = "Sesi\u00f3n cerrada exitosamente" });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    [HttpPut("change-password")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> ChangePassword(ChangePasswordDto dto)
    {
        var userIdFromToken = GetUserIdFromToken();
        if (userIdFromToken == null)
            return Unauthorized("No estás autorizado para cambiar la contraseña de este usuario.");
        dto.MustChangePassword = false;
        var response = await _authService.ChangePasswordAsync(userIdFromToken.Value, dto);
        if (response == null || !response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<Response<string>>> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        var response = await _authService.RequestPasswordResetAsync(dto);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<Response<string>>> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var response = await _authService.ResetPasswordAsync(dto);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("personnel/{token}")]
    public async Task<ActionResult<Response<PersonnelInfoDto>>> GetPersonnelInfo(string token)
    {
        if (string.IsNullOrEmpty(token))
            return Unauthorized("Token no proporcionado.");
        var response = await _authService.GetPersonnelInfoAsync(token);
        if (response == null || !response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("validate/{token}")]
    public async Task<ActionResult<Response<string>>> ValidatePersonnelToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return Unauthorized("Token no proporcionado.");
        var response = await _authService.VerifyTokenAsync(token);
        if (response == null || !response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("claims/orgApp/{orgAppId}/user/{userId}")]
    public async Task<ActionResult<Response<ClaimDto>>> GetClaims(Guid userId, int orgAppId)
    {
        if (userId == Guid.Empty || orgAppId <= 0)
            return BadRequest("User ID or Organization Application ID is invalid.");

        var response = await _authService.GetClaimsAsync(userId, orgAppId);
        if (response == null || !response.Succeeded)
            return BadRequest(response);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("{areaId}/areas")]
    public async Task<ActionResult<Response<List<AreaDto>>>> GetAreas(Guid areaId)
    {
        var response = await _authService.GetAreasForOrgByAreaIdAsync(areaId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("app-secret/{appId}")]
    public async Task<ActionResult<Response<string>>> GetOrgAppSecret([FromRoute] Guid appId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("super_admin"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var resp = await _authService.GetAppSecretAsync(appId);
        if (!resp.Succeeded) return BadRequest(resp);
        return Ok(resp);
    }

    [AllowAnonymous]
    [HttpPost("confirm-email")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<ConfirmEmailDto>>> ConfirmEmail([FromBody] ConfirmEmailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.Token))
            return BadRequest(new Response<ConfirmEmailDto>("El token de confirmación es requerido", false));

        var response = await _userService.ConfirmEmailAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("smtp-test")]
    public async Task<ActionResult> SmtpTest()
    {
        var result = await _emailService.TestSmtpConnectionAsync();
        return Ok(result);
    }

   

    [HttpDelete("sessions/{sessionId}")]
    public async Task<ActionResult<Response<string>>> RevokeSession(Guid sessionId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("super_admin"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _authService.RevokeSessionAsync(sessionId);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("sessions/application/{applicationId}")]
    public async Task<ActionResult<Response<string>>> RevokeSessionByApplicationId(Guid applicationId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("super_admin"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _authService.RevokeAllSessionsByApplicationIdAsync(applicationId);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("sessions")]
    public async Task<ActionResult<Response<string>>> RevokeAllSessions()
    {

        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("super_admin"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _authService.RevokeAllSessionsAsync();
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("sessions")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<SessionAdminDto>>> GetAllSessions(
        [FromQuery] SessionFilterDto? dto,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize
        )
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("super_admin"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _authService.GetAllSessionsAsync(pageNumber, pageSize, dto);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

}
