namespace GOBID.Application.ViewModels.AccessType;

public class AccessTypeVm
{
    public int AccessTypeId { get; set; }

    public string? AccessTypeName { get; set; }

    public string? AccessTypeDescription { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
