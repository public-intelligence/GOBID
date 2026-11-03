namespace GOBID.Application.ViewModels.ApplicationRoleClaim;

public class ApplicationRoleClaimVm
{
    public int ApplicationRoleClaimId { get; set; }
    public int ApplicationRoleId { get; set; }
    public int ClaimId { get; set; }
    public string? ClaimName { get; set; }
    public string? ClaimCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
