namespace GOBID.Application.ViewModels.Claim;

public class ClaimVm
{
    public int ClaimId { get; set; }

    public Guid ApplicationId { get; set; }

    public string? Code { get; set; }

    public string? ClaimName { get; set; }

    public string? ClaimDescription { get; set; }
    public bool IsAutomatic { get; set; }
    public bool IsActive { get; set; }
    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }
}
