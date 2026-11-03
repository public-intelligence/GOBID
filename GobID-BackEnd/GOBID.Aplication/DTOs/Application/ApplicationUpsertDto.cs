using GOBID.Application.DTOs.Claim;
using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Application;

public class ApplicationUpsertDto
{
    public Guid? ApplicationId { get; set; }

    public required string ApplicationName { get; set; }

    public required string Code { get; set; }

    public required string RedirectUrl { get; set; }

    public required string ApplicationDescription { get; set; }

    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    public string? RowVersion { get; set; }
    public ClaimUpsertDto[]? Claims { get; set; }
    public bool IsPublic { get; set; }
}
