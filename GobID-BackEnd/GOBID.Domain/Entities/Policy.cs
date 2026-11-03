namespace GOBID.Domain.Entities;

public class Policy
{
    public Guid PolicyId { get; set; }
    public int OrganizationId { get; set; }
    public int PolicyTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Effect { get; set; } = null!; // ALLOW | DENY
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual Organization Organization { get; set; } = null!;
    public virtual PolicyType PolicyType { get; set; } = null!;
    public virtual ICollection<PolicyCondition> PolicyConditions { get; set; } = new List<PolicyCondition>();
    public virtual ICollection<UserApplicationPolicy> UserApplicationPolicies { get; set; } = new List<UserApplicationPolicy>();
}
