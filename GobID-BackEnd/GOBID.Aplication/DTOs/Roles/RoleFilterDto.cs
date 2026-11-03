namespace GOBID.Application.DTOs.Roles;

public class RoleFilterDto
{
    public int? OrganizationApplicationId { get; set; }
    public bool? IsActive { get; set; }
    public int? RoleId { get; set; }
    public string? Search { get; set; }
    public bool? IsSystemRole { get; set; }
    public bool? ExcludePositionRoles { get; set; }
}