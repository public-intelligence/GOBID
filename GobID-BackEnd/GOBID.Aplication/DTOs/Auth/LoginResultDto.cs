using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Auth;

public class LoginResultDto
{
    [JsonProperty("user_id")]
    public Guid UserId { get; set; }

    [JsonProperty("personnel_id")]
    public Guid PersonnelId { get; set; }

    [JsonProperty("person_position_id")]
    public Guid? PersonPositionId { get; set; }

    [JsonProperty("organization_application_id")]
    public int OrganizationApplicationId { get; set; }
    [JsonProperty("organization_id")]
    public int OrganizationId { get; set; }
    [JsonProperty("organization_name")]
    public string OrganizationName { get; set; } = string.Empty;
    public string Acronym { get; set; } = string.Empty;
    [JsonProperty("application_id")]
    public Guid ApplicationId { get; set; }
    [JsonProperty("application_name")]
    public string ApplicationName { get; set; } = string.Empty;

    [JsonProperty("must_change_password")]
    public bool MustChangePassword { get; set; }

    [JsonProperty("user_password")]
    public string? UserPassword { get; set; } = string.Empty;

    [JsonProperty("org_context_selected")]
    public bool OrgContextSelected { get; set; }

    public List<ClaimDto> Claims { get; set; } = [];

    [JsonProperty("session_id")]
    public Guid? SessionId { get; set; }

    [JsonProperty("is_trusted_device")]
    public bool IsTrustedDevice { get; set; }

    [JsonProperty("user_application_id")]
    public Guid? UserApplicationId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int? RoleId { get; set; }
}
