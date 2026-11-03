namespace GOBID.Application.DTOs.Auth;

public class LoginRequestDto
{
    public required string Login { get; set; }
    public required string Password { get; set; }
    public string? Ip { get; set; }
    public string? Headers { get; set; }
    public Guid ApplicationId { get; set; }
    public bool IsTrustedDevice { get; set; }
}