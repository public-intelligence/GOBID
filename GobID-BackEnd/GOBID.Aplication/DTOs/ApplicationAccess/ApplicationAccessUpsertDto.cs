using Newtonsoft.Json;

namespace GOBID.Application.DTOs.ApplicationAccess;

public class ApplicationAccessUpsertDto
{
    public int? ApplicationAccessId { get; set; }

    public int OrganizationApplicationId { get; set; }

    public int AccessTypeId { get; set; }

    public required string ConfigurationJson { get; set; }

    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }
}
