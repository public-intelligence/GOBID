using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Domain.DTOs.PersonnelPositions;

public record PersonnelPositionDTO
{
    public Guid Id { get; init; }
    public Guid PersonId { get; init; }
    public Guid PositionId { get; init; }
    public Guid OrganizationStructureId { get; init; }
    public bool IsActive { get; init; }
}
