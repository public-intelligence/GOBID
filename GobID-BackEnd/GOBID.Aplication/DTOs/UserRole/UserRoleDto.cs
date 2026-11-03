using GOBID.Application.ViewModels.Roles;
using Newtonsoft.Json;

namespace GOBID.Application.DTOs.UserRole;

public class UserRoleDto
{
    [JsonProperty("user_role_id")]
    public int UserRoleId { get; set; }
    [JsonProperty("user_application_id")]
    public Guid UserApplicationId { get; set; }
    [JsonProperty("role_id")]
    public int RoleId { get; set; }
    [JsonProperty("user_role_description")]
    public string? UserRoleDescription { get; set; }
    [JsonProperty("role_name")]
    public string? RoleName { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
