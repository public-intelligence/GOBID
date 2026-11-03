using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GOBID.Application.DTOs.Organization;

public class OrganizationDto
{
    [JsonProperty("organization_id")]
    public int OrganizationId { get; set; }

    [JsonProperty("organization_name")]
    public required string OrganizationName { get; set; }

    [JsonProperty("acronym")]
    public required string Acronym { get; set; }

    [JsonProperty("organization_description")]
    public required string OrganizationDescription { get; set; }

    [JsonProperty("slogan")]
    public string? Slogan { get; set; }

    [JsonProperty("website")]
    public required string Website { get; set; }

    [JsonProperty("email")]
    public required string Email { get; set; }

    [JsonProperty("organization_address")]
    public required string OrganizationAddress { get; set; }

    [JsonProperty("phone")]
    public required string Phone { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("legal_entity_id")]
    public Guid? LegalEntityId { get; set; }

    [JsonProperty("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonProperty("created_by")]
    public Guid? CreatedBy { get; set; }

    [JsonProperty("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [JsonProperty("row_version")]
    public byte[]? RowVersion { get; set; }
}
