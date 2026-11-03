using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Application;

public class ApplicationDeleteDto
{
    public string? RowVersion { get; set; }
    public Guid UpdatedBy { get; set; }
}