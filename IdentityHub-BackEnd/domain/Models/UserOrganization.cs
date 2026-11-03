namespace IdentityHub.Domain.Models;

public class UserOrganization
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
}
