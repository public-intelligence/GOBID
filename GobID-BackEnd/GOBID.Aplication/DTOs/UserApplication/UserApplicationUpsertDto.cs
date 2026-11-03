using Newtonsoft.Json;

namespace GOBID.Application.DTOs.UserApplication;

public class UserApplicationUpsertDto
{
    public Guid? UserApplicationId { get; set; }
    public Guid? UserId { get; set; }
    public required int OrganizationApplicationId { get; set; }
    public string? UserApplicationDescription { get; set; }

    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsApplicationAdmin { get; set; }

    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; }
    public Guid? UpdatedBy { get; set; }

    public string? RowVersion { get; set; }
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public int? OrganizationId { get; set; }

    public List<Guid>? PolicyIds { get; set; }
}