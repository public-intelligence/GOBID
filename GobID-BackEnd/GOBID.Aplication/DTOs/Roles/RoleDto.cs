using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace GOBID.Application.DTOs.Roles;

public class RoleDto
{
    [JsonProperty("role_id")]
    public int RoleId { get; set; }

    [JsonProperty("role_name")]
    public required string RoleName { get; set; }

    [JsonProperty("role_description")]
    public required string RoleDescription { get; set; }

    [JsonProperty("set_automatic_claims")]
    public bool SetAutomaticClaims { get; set; }

    [JsonProperty("organization_application_id")]
    public int OrganizationApplicationId { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [JsonProperty("user_count")]
    public int? UserCount { get; set; }
    [JsonProperty("claim_count")]
    public int? ClaimCount { get; set; }
    
    [JsonProperty("is_system_role")]
    public bool IsSystemRole { get; set; }
    
    [JsonProperty("position_id")]
    public Guid? PositionId { get; set; }
}
