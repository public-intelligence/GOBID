namespace GOBID.Application.ViewModels.UserApplicationPolicy;

public class UserApplicationPolicyVm
{
    public Guid UserApplicationPolicyId { get; set; }
    public Guid UserApplicationId { get; set; }
    public Guid PolicyId { get; set; }
    public string? PolicyName { get; set; }
    public string? PolicyTypeName { get; set; }
    public string? Effect { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
