using Newtonsoft.Json;

namespace GOBID.Application.DTOs.ApplicationAccess;

public class ApplicationAccessDto
{
    [JsonProperty("application_access_id")]
    public int ApplicationAccessId { get; set; }

    [JsonProperty("organization_application_id")]
    public int OrganizationApplicationId { get; set; }

    [JsonProperty("access_type_id")]
    public int AccessTypeId { get; set; }

    [JsonProperty("configuration_json")]
    [JsonConverter(typeof(ConfigurationJsonConverter))]
    public object? ConfigurationJson { get; set; }

    [JsonProperty("is_active")]
    public bool IsActive { get; set; }

    [JsonProperty("created_at")]
    public DateTime? CreatedAt { get; set; }
    [JsonProperty("created_by")]
    public Guid? CreatedBy { get; set; }

    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [JsonProperty("updated_by")]
    public Guid? UpdatedBy { get; set; }
}
