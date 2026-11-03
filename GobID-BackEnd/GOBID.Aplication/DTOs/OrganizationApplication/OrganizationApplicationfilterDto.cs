namespace GOBID.Application.DTOs.OrganizationApplication;

public class OrganizationApplicationfilterDto
{
    public int? OrganizationId { get; set; }
    public Guid? ApplicationId { get; set; }
    public bool? IsActive { get; set; }
        public string? Search { get; set; }
}
