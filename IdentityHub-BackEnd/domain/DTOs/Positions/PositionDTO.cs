namespace IdentityHub.Domain.DTOs.Positions;

public record PositionDTO
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public bool IsManager { get; init; }
    public bool IsActive { get; init; } 
}
