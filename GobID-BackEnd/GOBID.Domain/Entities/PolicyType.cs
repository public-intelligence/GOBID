namespace GOBID.Domain.Entities;

public class PolicyType
{
    public int PolicyTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }

    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
