using GOBID.Application.DTOs.ApplicationAccess;
using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Claim;

public class ClaimResultWrapper
{
    [JsonProperty("data")]
    public List<ClaimDto>? Data { get; set; }

    [JsonProperty("total_tally")]
    public int TotalTally { get; set; }

    [JsonProperty("current_page")]
    public int CurrentPage { get; set; }

    [JsonProperty("total_pages")]
    public int TotalPages { get; set; }
}
