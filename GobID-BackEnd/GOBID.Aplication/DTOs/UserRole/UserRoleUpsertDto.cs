namespace GOBID.Application.DTOs.UserRole;

public class UserRoleUpsertDto
{
    public int? UserRoleId { get; set; }
    public Guid UserApplicationId { get; set; }
    public int RoleId { get; set; }
    public string? UserRoleDescription { get; set; }
    public Guid? UpdatedBy { get; set; }
}
