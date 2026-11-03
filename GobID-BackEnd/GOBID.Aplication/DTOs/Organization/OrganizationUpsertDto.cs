using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Organization;

public class OrganizationUpsertDto
{
    public int? OrganizationId { get; set; }
    public required string OrganizationName { get; set; }
    public required string Acronym { get; set; }
    public required string OrganizationDescription { get; set; }
    public string? Slogan { get; set; }
    public required string Website { get; set; }
    public required string Email { get; set; }
    public required string OrganizationAddress { get; set; }
    public required string Phone { get; set; }

    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; }
    
    public Guid? LegalEntityId { get; set; }

    [JsonConverter(typeof(ByteArrayToHexJsonConverter))]
    public byte[]? RowVersion { get; set; }
}
