using Newtonsoft.Json;

namespace GOBID.Application.DTOs.AccessType;

public class AccessTypeUpsertDto
{
    public int? AccessTypeId { get; set; }

    public required string AccessTypeName { get; set; }

    public required string AccessTypeDescription { get; set; }

    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; }
}