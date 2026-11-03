namespace GOBID.Application.DTOs.Auth;

public class ExchangeCodeDto
{
    public required string Code { get; set; }
    public required string RedirectUri { get; set; }
    public int OrganizationApplicationId { get; set; }
}
