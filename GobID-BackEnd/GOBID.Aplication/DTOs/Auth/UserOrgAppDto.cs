namespace GOBID.Application.DTOs.Auth;

public class UserOrgAppDto
{
    public int OrganizationApplicationId { get; set; }
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public string? ApplicationName { get; set; }
    public bool PolicyAccess { get; set; } = true;
    public string? PolicyMessage { get; set; }
}
