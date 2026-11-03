using Newtonsoft.Json;

namespace GOBID.Application.DTOs.OrganizationContact;

public class OrganizationContactUpsertDto
{
    public int? ContactId { get; set; }
    public int OrganizationId { get; set; }
    public required string OrganizationContactName { get; set; }
    public string? Position { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; }
}
