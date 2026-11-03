using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Domain.DTOs.PersonnelPositions;

public record CreatePersonnelPositionDTO
{
    public required Guid PersonId { get; init; }
    public required Guid PositionId { get; init; }
    public required Guid OrganizationStructureId { get; init; }
}
