namespace IdentityHub.Domain.DTOs.OrganizationalStructures;

public record OrganizationalStructureDTO
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; } 
    public Guid? ParentId { get; init; }
    public Guid AreaTypeId { get; init; }
    public string Name { get; init; } = null!;
    public bool IsActive { get; init; }

    public ICollection<OrganizationalStructureDTO> Dependencies { get; set; } = [];

}
