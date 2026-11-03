using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Claim;

public class ClaimDto
{
    [JsonProperty("claim_id")]
    public int ClaimId { get; set; }

    [JsonProperty("application_id")]
    public Guid ApplicationId { get; set; }

    public required string Code { get; set; }

    [JsonProperty("claim_name")]
    public required string ClaimName { get; set; }

    [JsonProperty("claim_description")]
    public required string ClaimDescription { get; set; }
    [JsonProperty("is_automatic")]
    public bool IsAutomatic { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("created_at")]    
    public DateTime CreatedAt { get; set; }

    [JsonProperty("created_by")]
    public Guid? CreatedBy { get; set; }

    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonProperty("updated_by")]
    public Guid? UpdatedBy { get; set; }
}