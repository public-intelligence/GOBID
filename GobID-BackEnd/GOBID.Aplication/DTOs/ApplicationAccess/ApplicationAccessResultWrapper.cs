using Newtonsoft.Json;

namespace GOBID.Application.DTOs.ApplicationAccess;

public class ApplicationAccessResultWrapper
{
    [JsonProperty("data")]
    public List<ApplicationAccessDto>? Data { get; set; }

    [JsonProperty("total_tally")]
    public int TotalTally { get; set; }

    [JsonProperty("current_page")]
    public int CurrentPage { get; set; }

    [JsonProperty("total_pages")]
    public int TotalPages { get; set; }
}