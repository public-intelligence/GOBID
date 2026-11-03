namespace GOBID.Application.DTOs.ApplicationRole;

public class ApplicationRoleUpsertDto
{
    public int? ApplicationRoleId { get; set; }
    public Guid AppCredentialId { get; set; }
    public int OrganizationApplicationId { get; set; }
    public int RoleId { get; set; }
    public string? Description { get; set; }
    public Guid? UpdatedBy { get; set; }
}
