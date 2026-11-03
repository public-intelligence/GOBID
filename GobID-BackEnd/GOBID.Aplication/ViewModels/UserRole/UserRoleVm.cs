using GOBID.Application.ViewModels.Roles;

namespace GOBID.Application.ViewModels.UserRole;

public class UserRoleVm
{
    public int UserRoleId { get; set; }
    public Guid UserApplicationId { get; set; }
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
    public string? UserRoleDescription { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
