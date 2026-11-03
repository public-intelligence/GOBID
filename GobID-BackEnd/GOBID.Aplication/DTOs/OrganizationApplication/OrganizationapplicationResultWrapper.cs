using Newtonsoft.Json;

namespace GOBID.Application.DTOs.OrganizationApplication;

public class OrganizationapplicationResultWrapper
{
    [JsonProperty("data")]
    public List<OrganizationApplicationDto>? Data { get; set; }

    [JsonProperty("total_tally")]
    public int TotalTally { get; set; }

    [JsonProperty("current_page")]
    public int CurrentPage { get; set; }

    [JsonProperty("total_pages")]
    public int TotalPages { get; set; }
}
