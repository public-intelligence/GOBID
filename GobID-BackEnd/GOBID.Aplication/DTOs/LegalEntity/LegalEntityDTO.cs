using GOBID.Application.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GOBID.Application.DTOs.EntityOrganization;

public record LegalEntityDTO
{
    public Guid Id { get; init; }
    public string LegalName { get; init; } = null!;
    public string SocialName { get; init; } = null!;
    public string IdentificationNumber { get; init; } = null!;
    public string? Email { get; init; }
    public string PhoneNumber { get; init; } = null!;
    public string Type { get; init; } = null!;
    public string StatusDGII { get; set; } = null!;

    [JsonConverter(typeof(StringEnumConverter))]
    public EnumStatus Status { get; init; }
}
