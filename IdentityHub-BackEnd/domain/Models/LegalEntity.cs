using IdentityHub.Domain.Enums;

namespace IdentityHub.Domain.Models;

public class LegalEntity
{
    public Guid Id { get; set ; }
    public required string LegalName { get; set; } 
    public required string SocialName { get; set; } 
    public string IdentificationNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public required string Type { get; set; }
    public required string StatusDGII { get; set; }
    public EnumStatus Status { get; set; }
}
