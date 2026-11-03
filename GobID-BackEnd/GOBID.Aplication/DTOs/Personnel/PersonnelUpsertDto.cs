namespace GOBID.Application.DTOs.Personnel;

public class PersonnelUpsertDto
{
    public Guid? PersonnelId { get; set; }
    public Guid PersonId { get; set; }

    public Guid? PositionId { get; set; }
    public Guid? LocalityId { get; set; }

    public required Guid PositionNominateId { get; set; }
    public int OrganizationId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
