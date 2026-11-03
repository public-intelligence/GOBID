namespace GOBID.Application.DTOs.Auth;

public class ForgotPasswordRequestDto
{
    public required string Email { get; set; }
    public Guid ApplicationId { get; set; }
}
