using Newtonsoft.Json;

namespace GOBID.Application.DTOs;

public class ResultWrapper<T>
{
    [JsonProperty("data")]
    public List<T>? Data { get; set; }

    [JsonProperty("totalTally")]
    public int TotalTally { get; set; }

    [JsonProperty("currentPage")]
    public int CurrentPage { get; set; }

    [JsonProperty("totalPages")]
    public int TotalPages { get; set; }

    [JsonProperty("pageSize")]
    public int PageSize { get; set; }
}
