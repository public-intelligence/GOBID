namespace GOBID.Application.DTOs.ApplicationAccess;

public class ApplicationAccessFilterDto
{
    public int? OrganizationApplicationId { get; set; }
    public int? AccessTypeId { get; set; }
    public bool? IsActive { get; set; }
}
