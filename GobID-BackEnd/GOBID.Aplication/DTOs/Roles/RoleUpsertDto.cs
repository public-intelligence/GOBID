using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Roles;

public class RoleUpsertDto
{
    public int? RoleId { get; set; }
    public required string RoleName { get; set; }
    public required string RoleDescription { get; set; }
    public bool SetAutomaticClaims { get; set; }
    public int OrganizationApplicationId { get; set; }
    public Guid? PositionId { get; set; }

    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsSystemRole { get; set; }
}