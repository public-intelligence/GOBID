namespace IdentityHub.Domain.DTOs.Positions;

public record UpdatePositionDTO
{
    public string? Name { get; set; }
    public bool? IsManager { get; set; }
    public bool? IsActive { get; set; }
}
