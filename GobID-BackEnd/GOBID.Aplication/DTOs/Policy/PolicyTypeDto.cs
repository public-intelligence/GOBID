using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Policies;

public class PolicyTypeDto
{
    [JsonProperty("policy_type_id")]
    public int PolicyTypeId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
}
