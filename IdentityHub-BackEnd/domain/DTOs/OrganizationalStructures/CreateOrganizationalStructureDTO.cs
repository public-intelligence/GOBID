namespace IdentityHub.Domain.DTOs.OrganizationalStructures;

public record CreateOrganizationalStructureDTO
{
    public required Guid OrganizationId { get; init; }
    public Guid AreaTypeId { get; init; }
    public Guid? ParentId { get; init; }
    public required string Name { get; init; }
}
