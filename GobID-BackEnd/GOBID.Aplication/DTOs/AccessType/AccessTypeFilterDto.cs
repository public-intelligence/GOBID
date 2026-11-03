using Newtonsoft.Json;

namespace GOBID.Application.DTOs.AccessType;

public class AccessTypeFilterDto
{
    public string? name { get; set; }

    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool? isActive { get; set; }
}
