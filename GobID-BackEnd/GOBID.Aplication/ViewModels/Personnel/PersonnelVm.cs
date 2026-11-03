using GOBID.Application.DTOs.Pds;
using GOBID.Application.DTOs.Personnel;

namespace GOBID.Application.ViewModels.Personnel;

public class PersonnelVm
{
    public Guid PersonnelId { get; set; }
    public Guid PersonId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? LocalityId { get; set; }
    public Guid? PositionNominateId { get; set; }
    public int OrganizationId { get; set; }
    public bool IsActive { get; set; }
    public int Level { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Información de la Posición desde PDS
    public PdsPositionDto? Position { get; set; }

    // Información de la Localidad desde PDS
    public PdsLocalidadDto? Locality { get; set; }

    // Información de la Persona desde Identity
    public IdentityPersonResponseDTO? Person { get; set; }
}
