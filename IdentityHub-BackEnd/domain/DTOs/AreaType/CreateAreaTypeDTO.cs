namespace IdentityHub.Domain.DTOs.AreaType;

public record CreateAreaTypeDTO
{
    public Guid? ParentId { get; init; }
    public required string Name { get; init; }
    public required int Order { get; init; }
}
