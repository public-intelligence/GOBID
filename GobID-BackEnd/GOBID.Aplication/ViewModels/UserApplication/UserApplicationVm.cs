using GOBID.Application.ViewModels.OrganizationApplication;
using GOBID.Application.ViewModels.User;
using GOBID.Application.ViewModels.UserRole;

namespace GOBID.Application.ViewModels.UserApplication;

public class UserApplicationVm
{
    public Guid UserApplicationId { get; set; }

    public Guid UserId { get; set; }

    public int OrganizationApplicationId { get; set; }

    public string? UserApplicationDescription { get; set; }

    public bool IsApplicationAdmin { get; set; }

    public string? InternalAppUserCode { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public string? RowVersion { get; set; }

    public OrganizationApplicationVm? OrganizationApplication { get; set; }

    public UserVm? User { get; set; }
    public UserRoleVm? UserRole { get; set; }
}