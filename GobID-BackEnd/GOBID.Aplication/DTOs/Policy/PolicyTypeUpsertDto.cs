namespace GOBID.Application.DTOs.Policies;

public class PolicyTypeUpsertDto
{
    public int? PolicyTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
}
