namespace IdentityHub.Domain.DTOs.UserOrganizations;

public record UserOrganizationsDTO
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }

}
