using Newtonsoft.Json;

namespace GOBID.Application.DTOs.OrganizationApplication;

public class OrganizationApplicationUpsertDto
{
    public int? OrganizationApplicationId { get; set; }
    public int OrganizationId { get; set; }
    public Guid ApplicationId { get; set; }
    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; }

    [JsonConverter(typeof(ByteArrayToHexJsonConverter))]
    public string? RowVersion { get; set; } 
}
