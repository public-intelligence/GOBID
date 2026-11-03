using System.Text.Json.Serialization;

namespace GOBID.Application.DTOs.UserApplication;

public class UserApplicationFilterDto
{
    public Guid? UserId { get; set; }
    public int? OrganizationApplicationId { get; set; }
    public int? OrganizationId { get; set; }
    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool? IsActive { get; set; }

}
