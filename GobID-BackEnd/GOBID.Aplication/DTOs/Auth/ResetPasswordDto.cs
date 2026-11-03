namespace GOBID.Application.DTOs.Auth;

public record ResetPasswordDto
{
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmNewPassword { get; set; }
}
