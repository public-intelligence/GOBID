using GOBID.Application.DTOs.Personnel;

namespace GOBID.Application.DTOs.Auth;

public class SessionAdminDto
{
    public Guid SessionId { get; set; }
    /// <summary>NULL para sesiones M2M (service accounts).</summary>
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? IdentificationNumber { get; set; }
    public Guid ApplicationId { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime AbsoluteExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
