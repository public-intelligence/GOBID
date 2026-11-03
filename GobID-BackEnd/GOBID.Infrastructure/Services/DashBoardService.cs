using Dapper;
using GOBID.Application.DTOs;
using GOBID.Application.DTOs.DashBoard;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using Newtonsoft.Json;
using System.Data;

namespace GOBID.Infrastructure.Services;

public class DashBoardService: IDashboardService
{
    private readonly IDbConnection _connection;

    public DashBoardService(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<Response<PrincipalMetricsDto>> GetPrincipalMetricsAsync(bool? excludeOrg, int? orgId)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_summary_counts",
                new { excludeOrg, organizationId = orgId },
                commandType: CommandType.StoredProcedure);
            var principalMetrics = JsonConvert.DeserializeObject<PrincipalMetricsDto>(result ?? "{}");
            if (principalMetrics == null)
                return new Response<PrincipalMetricsDto>("No se pudo obtener los datos del dashboard", false);
            return new Response<PrincipalMetricsDto>(principalMetrics, "OK");
        }
        catch (Exception)
        {
            return new Response<PrincipalMetricsDto>($"Error al obtener los datos", false);
        }
    }

    public async Task<Response<List<SuccessfulLoginsByMonthDto>>> GetMonthlyRegistrationsAsync(int? orgId)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_successful_logins_by_month",
                new { organization_id = orgId },
                commandType: CommandType.StoredProcedure);
            var monthlyRegistrations = JsonConvert.DeserializeObject<List<SuccessfulLoginsByMonthDto>>(result ?? "[]");
            if (monthlyRegistrations == null)
                return new Response<List<SuccessfulLoginsByMonthDto>>("No se pudo obtener los datos del dashboard", false);
            return new Response<List<SuccessfulLoginsByMonthDto>>(monthlyRegistrations, "OK");
        }
        catch (Exception)
        {
            return new Response<List<SuccessfulLoginsByMonthDto>>($"Error al obtener los datos", false);
        }
    }
}
