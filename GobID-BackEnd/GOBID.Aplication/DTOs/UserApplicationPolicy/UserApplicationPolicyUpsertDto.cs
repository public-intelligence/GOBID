namespace GOBID.Application.DTOs.UserApplicationPolicy;

public class UserApplicationPolicyUpsertDto
{
    public Guid? UserApplicationPolicyId { get; set; }
    public Guid UserApplicationId { get; set; }
    public Guid PolicyId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
