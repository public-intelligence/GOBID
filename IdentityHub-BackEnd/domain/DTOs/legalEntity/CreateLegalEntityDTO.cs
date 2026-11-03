using IdentityHub.Domain.Enums;

namespace IdentityHub.Domain.DTOs.Organizations;

public record CreateLegalEntityDTO
{
    public required string LegalName { get; set ; } 
    public required string SocialName { get; set; } 
    public required string IdentificationNumber { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; } 
    public required string Type { get; set; }
    public required string StatusDGII { get; set; }
    public EnumStatus Status { get; set; }
}
