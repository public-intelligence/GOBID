
using GOBID.Application.DTOs.DashBoard;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IDashboardService
{
    public Task<Response<PrincipalMetricsDto>> GetPrincipalMetricsAsync(bool? excludeOrg, int? orgId);
    public Task<Response<List<SuccessfulLoginsByMonthDto>>> GetMonthlyRegistrationsAsync(int? orgId);
}
