namespace GOBID.Application.DTOs.ApplicationRoleClaim;

public class CreateApplicationRoleClaimDto
{
    public int ApplicationRoleId { get; set; }

    public List<int> ClaimIds { get; set; } = [];
}

public class UpdateApplicationRoleClaimDto
{
    public bool IsActive { get; set; }
}
