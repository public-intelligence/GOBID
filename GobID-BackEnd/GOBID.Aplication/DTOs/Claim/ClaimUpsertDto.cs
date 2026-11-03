using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Claim;

public class ClaimUpsertDto
{
    public int? ClaimId { get; set; }
    public Guid? ApplicationId { get; set; }
    public required string Code { get; set; }
    public required string ClaimName { get; set; } 
    public string? ClaimDescription { get; set; }
    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsAutomatic { get; set; }
    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; } 
    public Guid? UpdatedBy { get; set; }
}
