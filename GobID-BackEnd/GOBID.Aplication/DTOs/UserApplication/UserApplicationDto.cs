using GOBID.Application.DTOs.UserRole;
using GOBID.Application.ViewModels.UserRole;
using Newtonsoft.Json;

namespace GOBID.Application.DTOs.UserApplication;

public class UserApplicationDto
{
    [JsonProperty("user_application_id")]
    public Guid UserApplicationId { get; set; }
    [JsonProperty("user_id")]
    public Guid UserId { get; set; }
    [JsonProperty("organization_application_id")]
    public int OrganizationApplicationId { get; set; }
    [JsonProperty("user_application_description")]
    public required string UserApplicationDescription { get; set; }
    [JsonProperty("is_application_admin")]
    public bool IsApplicationAdmin { get; set; }
    [JsonProperty("internal_app_user_code")]
    public string? InternalAppUserCode { get; set; }
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
    [JsonProperty("row_version")]
    public string? RowVersion { get; set; }

    public UserRoleVm? UserRole { get; set; } 
}
