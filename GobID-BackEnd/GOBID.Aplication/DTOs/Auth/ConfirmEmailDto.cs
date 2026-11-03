namespace GOBID.Application.DTOs.Auth;

public record ConfirmEmailDto
{
    public required string Token { get; set; }
}
