using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Auth;

public class ClaimDto
{
    [JsonProperty("claim_code")]
    public string? ClaimCode { get; set; } 
}
