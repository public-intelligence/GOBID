using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Domain.DTOs.PersonnelPositions;

public record UpdatePersonnelPositionDTO
{
    public Guid? PersonId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? OrganizationStructureId { get; set; }
    public bool? IsActive { get; set; }
}
