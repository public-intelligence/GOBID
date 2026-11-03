namespace GOBID.Application.ViewModels.Policies;

public class PolicyConditionVm
{
    public int PolicyConditionId { get; set; }
    public string ConditionKey { get; set; } = null!;
    public string ConditionValue { get; set; } = null!;
}

public class PolicyVm
{
    public Guid PolicyId { get; set; }
    public int OrganizationId { get; set; }
    public int PolicyTypeId { get; set; }
    public string? PolicyTypeName { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Effect { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<PolicyConditionVm> Conditions { get; set; } = new();
}
