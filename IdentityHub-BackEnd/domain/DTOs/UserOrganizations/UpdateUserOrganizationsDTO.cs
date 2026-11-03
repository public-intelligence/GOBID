using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Domain.DTOs.UserOrganizations;

public record UpdateUserOrganizationsDTO
{
    public  Guid? OrganizationId { get; set; }
    public  Guid? UserId { get; set; }
}
