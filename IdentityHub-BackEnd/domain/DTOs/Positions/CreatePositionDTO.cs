namespace IdentityHub.Domain.DTOs.Positions;

public record CreatePositionDTO
{
    public required string Name { get; init; }
    public bool IsManager { get; init; }
}
