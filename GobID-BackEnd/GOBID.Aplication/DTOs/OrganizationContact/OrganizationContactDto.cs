using Newtonsoft.Json;

namespace GOBID.Application.DTOs.OrganizationContact;

public class OrganizationContactDto
{
    [JsonProperty("contact_id")]    
    public int ContactId { get; set; }
    [JsonProperty("organization_id")]
    public int OrganizationId { get; set; }
    [JsonProperty("organization_contact_name")]
    public required string OrganizationContactName { get; set; }
    public string? Position { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }
    [JsonProperty("is_active")]
    public bool IsActive { get; set; }
    [JsonProperty("created_at")]
    public DateTime? CreatedAt { get; set; }
    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
