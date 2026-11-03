using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Pds;

public class PdsPositionSupervisorLevelDto
{
    [JsonProperty("level")]
    public int Level { get; set; }

    [JsonProperty("positions")]
    public List<PdsPositionDto> Positions { get; set; } = [];
}
