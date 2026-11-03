using GOBID.Application.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GOBID.Application.DTOs.EntityOrganization;

public record UpdateLegalEntityDTO
{
    public string? LegalName { get; set; }

    public string? SocialName { get; set; }

    public string? IdentificationNumber { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? StatusDGII { get; set; } 
    public string? Type { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public EnumStatus? Status { get; set; }
}
