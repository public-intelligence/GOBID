using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Domain.DTOs.UserOrganizations;

public record CreateUserOrganizationsDTO
{
    public required Guid OrganizationId { get; init; }
    public required Guid UserId { get; init; }
}
