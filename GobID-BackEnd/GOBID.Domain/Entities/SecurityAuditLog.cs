namespace GOBID.Domain.Entities;

public class SecurityAuditLog
{
    public long AuditLogId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid? ApplicationId { get; set; }
    public string? CorrelationId { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
    public bool Success { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class SecurityAuditEventTypes
{
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailure = "LOGIN_FAILURE";
    public const string MfaSuccess = "MFA_SUCCESS";
    public const string MfaFailure = "MFA_FAILURE";
    public const string TokenIssued = "TOKEN_ISSUED";
    public const string TokenRefreshed = "TOKEN_REFRESHED";
    public const string TokenRevoked = "TOKEN_REVOKED";
    public const string AuthzFailure = "AUTHZ_FAILURE";

    // M2M (Machine-to-Machine / service accounts)
    public const string AppLoginSuccess = "APP_LOGIN_SUCCESS";
    public const string AppLoginFailure = "APP_LOGIN_FAILURE";
    public const string AppTokenRefreshed = "APP_TOKEN_REFRESHED";
    public const string AppTokenRevoked = "APP_TOKEN_REVOKED";
}
