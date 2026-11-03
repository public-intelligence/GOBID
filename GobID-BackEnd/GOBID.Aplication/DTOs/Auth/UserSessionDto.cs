namespace GOBID.Application.DTOs.Auth;

public class UserSessionDto
{
    public Guid SessionId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
    public string? Ip { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsCurrentSession { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime AbsoluteExpiresAt { get; set; }
}
