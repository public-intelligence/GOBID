using GOBID.Application.ViewModels.Application;
using GOBID.Application.ViewModels.Organization;
using GOBID.Application.ViewModels.Roles;

namespace GOBID.Application.ViewModels.OrganizationApplication;

public class OrganizationApplicationVm
{
    public int OrganizationApplicationId { get; set; }

    public int OrganizationId { get; set; }
    public string? OrganizationName { get; set; } = null;

    public Guid ApplicationId { get; set; }

    public bool IsActive { get; set; }

    public string? RowVersion { get; set; }
    public OrganizationVm? Organization { get; set; }
    public ApplicationVm? Application { get; set; }
    public RoleVm[]? Role { get; set; }
    public int? RoleCount { get; set; }
}