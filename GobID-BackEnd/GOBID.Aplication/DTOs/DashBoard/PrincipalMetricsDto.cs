namespace GOBID.Application.DTOs.DashBoard;

public record PrincipalMetricsDto
{
    public int? TotalOrganizations { get; set; }
    public int TotalUsers { get; set; }
    public int TotalApplications { get; set; }
}
