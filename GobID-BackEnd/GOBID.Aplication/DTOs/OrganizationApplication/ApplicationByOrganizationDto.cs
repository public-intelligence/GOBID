using GOBID.Application.ViewModels.Application;
using Newtonsoft.Json;

namespace GOBID.Application.DTOs.OrganizationApplication;

public class ApplicationByOrganizationDto
{
    [JsonProperty("organization_application_id")]
    public int OrganizationApplicationId { get; set; }

    [JsonProperty("organization_id")]
    public int OrganizationId { get; set; }

    [JsonProperty("application_id")]
    public Guid ApplicationId { get; set; }

    public ApplicationVm? Application { get; set; }
}
