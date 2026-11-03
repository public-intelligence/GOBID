using IdentityHub.Domain.Enums;

namespace IdentityHub.Domain.DTOs.Organizations;

public record UpdateOrganizationDTO
{
    public string? LegalName { get; set; }
    public string? SocialName { get; set; }
    public string? IdentificationNumber { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Type { get; set ; }
    public string? StatusDGII { get; set; }
    public EnumStatus? Status { get; set; }
}
