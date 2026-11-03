namespace GOBID.Application.DTOs.Auth;

public class TokenPairDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public Guid? SessionId { get; set; }
}
