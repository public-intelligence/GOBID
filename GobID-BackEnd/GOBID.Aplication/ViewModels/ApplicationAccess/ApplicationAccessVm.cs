namespace GOBID.Application.ViewModels.ApplicationAccess;

public class ApplicationAccessVm
{
    public int ApplicationAccessId { get; set; }

    public int OrganizationApplicationId { get; set; }

    public int AccessTypeId { get; set; }

    public object? ConfigurationJson { get; set; }

    public bool IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

}
