#nullable disable
using System;

namespace GOBID.Domain.Entities;

public class UserOrganization
{
    public Guid UserOrganizationsId { get; set; }

    public int OrganizationId { get; set; }

    public Guid UserId { get; set; }

    public string Email { get; set; }

    public string Position { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual User User { get; set; }

    public virtual Organization Organization { get; set; }
}
