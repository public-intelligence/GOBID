#nullable disable
namespace GOBID.Domain.Entities;

public class Personnel
{
    public Guid PersonnelId { get; set; }
    public Guid PersonId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? LocalityId { get; set; }
    public Guid? PositionNominateId { get; set; }
    public int OrganizationId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public virtual Organization Organization { get; set; }
}
