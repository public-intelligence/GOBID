using System.Text.Json.Serialization;

namespace GOBID.Application.DTOs.Roleclaim;

public class RoleClaimUpsertDto
{
    public int RoleId { get; set; }
    public int ClaimId { get; set; }

    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}