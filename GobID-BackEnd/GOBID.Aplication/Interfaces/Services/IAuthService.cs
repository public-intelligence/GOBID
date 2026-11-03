using GOBID.Application.DTOs.Auth;
using GOBID.Application.Wrappers;


namespace GOBID.Application.Interfaces.Services;

public interface IAuthService
{
    Task<string> GenerateJwtTokenAsync(LoginResultDto loginResult);
    Task<Response<LoginResultDto>> LoginJwtAsync(LoginRequestDto loginDto);
    Task<Response<string>> GenerateAuthCodeAsync(LoginResultDto loginResult);
    Task<Response<TokenPairDto>> ExchangeCodeForTokensAsync(ExchangeCodeDto dto);
    Task<Response<TokenPairDto>> RefreshAccessTokenAsync(string refreshToken);

    Task<Response<string>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<Response<PersonnelInfoDto>> GetPersonnelInfoAsync(string token);

    Task<Response<string>> VerifyTokenAsync(string token);
    Task<Response<ClaimDto>> GetClaimsAsync(Guid userId, int orgAppId);

    Task<Response<List<AreaDto>>> GetAreasForOrgByAreaIdAsync(Guid areaId);

    Task<Response<string>> GetAppSecretAsync(Guid appId);
    Task<Response<string>> RequestPasswordResetAsync(ForgotPasswordRequestDto dto);
    Task<Response<string>> ResetPasswordAsync(ResetPasswordDto dto);

    Task<Response<List<UserOrgAppDto>>> GetUserOrgAppsAsync(Guid userId, Guid applicationId, string? clientIp = null, string? userAgent = null);
    Task<string> GeneratePasswordResetTokenAsync(Guid userId, Guid applicationId);
    Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, string email);
    Task<Response<Guid>> ValidateEmailConfirmationTokenAsync(string token);
    Task<Response<string>> RevokeRefreshTokenAsync(string refreshToken);

    Task<Response<List<UserSessionDto>>> GetUserSessionsAsync(Guid userId, Guid applicationId, Guid? currentSessionId = null);
    Task<Response<string>> RevokeSessionAsync(Guid sessionId);
    Task<Response<string>> RevokeAllSessionsByApplicationIdAsync(Guid applicationId);
    Task<Response<string>> RevokeAllUserSessionsAsync(Guid userId, Guid applicationId);
    Task<Response<string>> RevokeAllSessionsAsync();
    Task<bool> IsSessionActiveAsync(Guid sessionId);
    Task<PagedResponse<SessionAdminDto>> GetAllSessionsAsync(int? pageNumber, int? pageSize, SessionFilterDto? filter);

    // ── M2M (Machine-to-Machine / service accounts) ─────────────────────────
    /// <summary>
    /// Autentica una aplicación usando client_id + client_secret (Client Credentials Grant).
    /// Devuelve accessToken + refreshToken directamente en el body (no cookie).
    /// </summary>
    Task<Response<AppTokenResponseDto>> AppLoginAsync(AppLoginRequestDto dto);

    /// <summary>
    /// Refresca los tokens de una sesión M2M.
    /// Aplica rotación: el refresh token anterior queda revocado.
    /// </summary>
    Task<Response<AppTokenResponseDto>> RefreshAppTokenAsync(string refreshToken);

    /// <summary>
    /// Valida un token JWT emitido por el flujo M2M (Client Credentials Grant).
    /// Verifica firma, vigencia y que el claim <c>is_service_account</c> sea <c>true</c>.
    /// </summary>
    Task<Response<string>> VerifyAppTokenAsync(string token);
}
