namespace GOBID.Application.ViewModels.Policies;

public class PolicyTypeVm
{
    public int PolicyTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
