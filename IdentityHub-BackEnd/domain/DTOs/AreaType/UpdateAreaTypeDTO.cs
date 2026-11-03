namespace IdentityHub.Domain.DTOs.AreaType;

public record UpdateAreaTypeDTO
{
    public Guid? ParentId { get; init; }
    public string? Name { get; init; }
    public int? Order { get; init; }
}
