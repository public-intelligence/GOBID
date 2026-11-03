using GOBID.Application.ViewModels.Roles;
using Newtonsoft.Json;

namespace GOBID.Application.ViewModels.Application;

public class ApplicationVm
{
    public Guid ApplicationId { get; set; }

    public string? ApplicationName { get; set; } 

    public string? Code { get; set; } 

    public string? RedirectUrl { get; set; }

    public string? ApplicationDescription { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public string? RowVersion { get; set; }
    public bool IsPublic { get; set; }
}
