using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Policies;

public class PolicyConditionDto
{
    [JsonProperty("policy_condition_id")]
    public int PolicyConditionId { get; set; }

    [JsonProperty("condition_key")]
    public string ConditionKey { get; set; } = null!;

    [JsonProperty("condition_value")]
    public string ConditionValue { get; set; } = null!;
}
