using GOBID.Application.DTOs.Application;
using GOBID.Application.DTOs.Organization;
using GOBID.Application.ViewModels.Application;
using GOBID.Application.ViewModels.Organization;
using GOBID.Application.ViewModels.Roles;
using Newtonsoft.Json;

namespace GOBID.Application.DTOs.OrganizationApplication;

public class OrganizationApplicationDto
{
    [JsonProperty("organization_application_id")]
    public int OrganizationApplicationId { get; set; }

    [JsonProperty("organization_id")]
    public int OrganizationId { get; set; }
    [JsonProperty("organization_name")]
    public string? OrganizationName { get; set; }

    [JsonProperty("application_id")]
    public Guid ApplicationId { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("row_version")]
    public string? RowVersion { get; set; }

    public OrganizationVm? Organization { get; set; }
    public ApplicationVm? Application { get; set; }
    public RoleVm[]? Roles { get; set; }
}
