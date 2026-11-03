using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Application;

public class ApplicationDto
{
    [JsonProperty("application_id")]
    public Guid ApplicationId { get; set; }

    [JsonProperty("application_name")]
    public required string ApplicationName { get; set; }
    public required string Code { get; set; }

    [JsonProperty("redirect_url")]
    public required string RedirectUrl { get; set; }

    [JsonProperty("application_description")]
    public required string ApplicationDescription { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("created_by")]
    public Guid? CreatedBy { get; set; }

    [JsonProperty("created_at")]
    public DateTime? CreatedAt { get; set; }

    [JsonProperty("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonProperty("row_version")]
    public byte[]? RowVersion { get; set; }
    [JsonProperty("is_public")]
    public bool IsPublic { get; set; }
}
