namespace IdentityHub.Domain.DTOs.OrganizationalStructures;

public record UpdateOrganizationalStructureDTO
{
    public Guid? OrganizationId { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? AreaTypeId { get; init; }
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
}
