namespace GOBID.Application.ViewModels.Application;

public class ApplicationUpsertResponseVm
{
    public Guid ApplicationId { get; set; }

    public required string RowVersion { get; set; }
}
