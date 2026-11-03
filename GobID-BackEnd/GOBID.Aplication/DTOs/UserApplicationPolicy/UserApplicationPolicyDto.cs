using Newtonsoft.Json;

namespace GOBID.Application.DTOs.UserApplicationPolicy;

public class UserApplicationPolicyDto
{
    [JsonProperty("user_application_policy_id")]
    public Guid UserApplicationPolicyId { get; set; }

    [JsonProperty("user_application_id")]
    public Guid UserApplicationId { get; set; }

    [JsonProperty("policy_id")]
    public Guid PolicyId { get; set; }

    [JsonProperty("policy_name")]
    public string? PolicyName { get; set; }

    [JsonProperty("policy_type_name")]
    public string? PolicyTypeName { get; set; }

    [JsonProperty("effect")]
    public string? Effect { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
}
