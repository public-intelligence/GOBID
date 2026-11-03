using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.DashBoard;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class DashBoardController : OwnBaseController
{
    private readonly IDashboardService _dashboardService;
    public DashBoardController(IAuthService authService, IDashboardService dashboardService) : base(authService)
    {
        _dashboardService = dashboardService;
    }


    [HttpGet("metrics")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<PrincipalMetricsDto>>> GetMetrics()
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool? excludeOrg = false;
        int? orgId = null;
        if (permissions.Contains("admin_local"))
        {
            excludeOrg = true;
            orgId = GetOrganizationIdFromToken();
            if (orgId == null)
                return Unauthorized("No se pudo obtener el ID de la organización desde el token.");
        }
        
        var response = await _dashboardService.GetPrincipalMetricsAsync(excludeOrg, orgId);
        if (response == null || !response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("registrations")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<SuccessfulLoginsByMonthDto>>>> GetMonthlyRegistrations()
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        int? orgId = null;
        if (permissions.Contains("admin_local"))
        {
            orgId = GetOrganizationIdFromToken();
            if (orgId == null)
                return Unauthorized("No se pudo obtener el ID de la organización desde el token.");
        }
        var response = await _dashboardService.GetMonthlyRegistrationsAsync(orgId);
        if (response == null || !response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
}
