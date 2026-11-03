namespace GOBID.Application.ViewModels.Roles;

public class RoleVm
{
    public int RoleId { get; set; }

    public required string RoleName { get; set; }

    public required string RoleDescription { get; set; }

    public bool SetAutomaticClaims { get; set; }

    public int OrganizationApplicationId { get; set; }

    public Guid? PositionId { get; set; }

    public bool IsActive { get; set; }
    public bool IsSystemRole { get; set; }
    public int? UserCount { get; set; }
    public int? ClaimCount { get; set; }

}
