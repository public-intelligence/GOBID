using Newtonsoft.Json;

namespace GOBID.Application.DTOs.AccessType;

public class AccessTypeDto
{
    [JsonProperty("access_type_id")]
    public int AccessTypeId { get; set; }

    [JsonProperty("access_type_name")]
    public required string AccessTypeName { get; set; }

    [JsonProperty("access_type_description")]
    public required string AccessTypeDescription { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("created_by")]
    public Guid? CreatedBy { get; set; }

    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonProperty("updated_by")]
    public Guid? UpdatedBy { get; set; }
}