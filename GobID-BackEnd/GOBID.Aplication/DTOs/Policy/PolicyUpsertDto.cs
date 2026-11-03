namespace GOBID.Application.DTOs.Policies;

public class PolicyUpsertDto
{
    public Guid? PolicyId { get; set; }
    public int OrganizationId { get; set; }
    public int PolicyTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Effect { get; set; } = "ALLOW";
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public List<PolicyConditionUpsertDto> Conditions { get; set; } = new();
}
