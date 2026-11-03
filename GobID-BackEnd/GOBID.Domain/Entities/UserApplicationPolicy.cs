namespace GOBID.Domain.Entities;

public class UserApplicationPolicy
{
    public Guid UserApplicationPolicyId { get; set; }
    public Guid UserApplicationId { get; set; }
    public Guid PolicyId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public virtual UserApplication UserApplication { get; set; } = null!;
    public virtual Policy Policy { get; set; } = null!;
}
