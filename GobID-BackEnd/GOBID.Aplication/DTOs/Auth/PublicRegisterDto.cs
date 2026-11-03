namespace GOBID.Application.DTOs.Auth;

public class PublicRegisterDto
{
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
    public required Guid PersonnelId { get; set; }
}
