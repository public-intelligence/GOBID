using IdentityHub.Domain.Enums;

namespace IdentityHub.Domain.DTOs.Organizations;

public record LegalEntityDTO
{
    public Guid Id { get; init; }
    public string LegalName { get; init; } = null!;
    public string SocialName { get; init; } = null!;
    public string IdentificationNumber { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string PhoneNumber { get; init; } = null!;
    public string Type { get; init; } = null!;
    public required string StatusDGII { get; set; }
    public EnumStatus Status { get; init; }
}
