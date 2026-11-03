using Newtonsoft.Json;

namespace GOBID.Application.DTOs.User;

public class UserStatusUpdateDto
{
    public Guid UserId { get; set; }
    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; }
    public Guid? UpdatedBy { get; set; }
}
