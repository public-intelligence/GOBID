using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Application;

public class ApplicationFilterDto
{
    public Guid? ApplicationId { get; set; }
    public string? Code { get; set; }
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsPublic { get; set; }
}
