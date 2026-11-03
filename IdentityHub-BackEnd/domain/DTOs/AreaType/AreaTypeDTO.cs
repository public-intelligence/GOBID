namespace IdentityHub.Domain.DTOs.AreaType;

public record AreaTypeDTO
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Name { get; init; } = null!;
    public int Order { get; init; }

}
