namespace GOBID.Infrastructure.Settings;

public class PdsApiSettings
{
    public const string SectionName = "PdsApiSettings";
    public required string BaseUrl { get; init; }
}
