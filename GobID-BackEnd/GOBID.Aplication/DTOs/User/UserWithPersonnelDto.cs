using GOBID.Application.DTOs.Personnel;
using GOBID.Application.DTOs.Pds;

namespace GOBID.Application.DTOs.User;

public class UserWithPersonnelDto
{
    public Guid PersonnelId { get; set; }
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public Guid UserApplicationId { get; set; }
    public int OrganizationApplicationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public IdentityPersonResponseDTO? Personnel { get; set; }
    public Guid? PositionId { get; set; }
    public PdsPositionDto? Position { get; set; }
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
}
