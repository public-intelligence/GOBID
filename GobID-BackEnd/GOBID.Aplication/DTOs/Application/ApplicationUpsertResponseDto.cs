using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Application;

public class ApplicationUpsertResponseDto
{
    [JsonProperty("application_id")]
    public Guid ApplicationId { get; set; }

    [JsonProperty("row_version")]
    public required string RowVersion { get; set; } 
}