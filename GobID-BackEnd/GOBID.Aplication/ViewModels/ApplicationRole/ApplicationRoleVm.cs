namespace GOBID.Application.ViewModels.ApplicationRole;

public class ApplicationRoleVm
{
    public int ApplicationRoleId { get; set; }
    public Guid AppCredentialId { get; set; }
    public int OrganizationApplicationId { get; set; }
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
