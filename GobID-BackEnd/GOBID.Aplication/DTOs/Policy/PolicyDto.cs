using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Policies;

public class PolicyDto
{
    [JsonProperty("policy_id")]
    public Guid PolicyId { get; set; }

    [JsonProperty("organization_id")]
    public int OrganizationId { get; set; }

    [JsonProperty("policy_type_id")]
    public int PolicyTypeId { get; set; }

    [JsonProperty("policy_type_name")]
    public string? PolicyTypeName { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("effect")]
    public string Effect { get; set; } = null!;

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonProperty("conditions")]
    public List<PolicyConditionDto> Conditions { get; set; } = new();
}
