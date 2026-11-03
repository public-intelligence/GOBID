using GOBID.Application.DTOs.EntityOrganization;
using GOBID.Application.ViewModels.OrganizationContact;

namespace GOBID.Application.ViewModels.Organization;

public class OrganizationVm
{
    public int OrganizationId { get; set; }

    public string OrganizationName { get; set; } = null!;

    public string Acronym { get; set; } = null!;

    public string OrganizationDescription { get; set; } = null!;

    public string Slogan { get; set; } = null!;

    public string Website { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string OrganizationAddress { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public bool IsActive { get; set; }
    public Guid LegalEntityId { get; set; }
    public LegalEntityDTO? LegalEntity { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public string? RowVersion { get; set; }
    public int? Count { get; set; }
    public OrganizationContactVm[]? Contact { get; set; }
}
