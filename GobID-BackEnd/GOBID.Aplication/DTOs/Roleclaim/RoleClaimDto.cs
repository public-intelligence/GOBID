using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Roleclaim;

public class RoleClaimDto
{
    [JsonProperty("role_claim_id")]
    public int RoleClaimId { get; set; }
    [JsonProperty("role_id")]
    public int RoleId { get; set; }
    [JsonProperty("claim_id")]
    public int ClaimId { get; set; }
    [JsonProperty("claim_name")]
    public required string ClaimName { get; set; }
    [JsonProperty("is_active")]
    public bool IsActive { get; set; }
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }
    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }

}