using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.OrganizationApplication;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.OrganizationApplication;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class OrganizationApplicationController : OwnBaseController
{
    private readonly IOrganizationApplicationService _organizationApplicationService;
    public OrganizationApplicationController(IOrganizationApplicationService organizationApplicationService, IAuthService authService
    ) : base(authService)
    {
        _organizationApplicationService = organizationApplicationService;
    }

    [HttpGet()]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<OrganizationApplicationVm>>> GetOrganizationApplications(
        int? pageNumber,
         int? pageSize,
        [FromQuery] OrganizationApplicationfilterDto? filter)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_org_app"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _organizationApplicationService.ListAsync(pageNumber, pageSize, filter);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("{orgAppId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<OrganizationApplicationVm>>> GetOrganizationApplicationById(int orgAppId)
    {
        var response = await _organizationApplicationService.GetAsync(1, 100, orgAppId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPut()]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> Upsert(OrganizationApplicationUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_org_app"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _organizationApplicationService.UpsertAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{orgAppId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> Delete(int orgAppId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("delete_org_app"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _organizationApplicationService.DeleteAsync(orgAppId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("Organization/{organizationId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<OrganizationApplicationVm>>> GetByOrganizationId(int? pageNumber,
         int? pageSize, int organizationId, string? search)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool? exludeApp = null;
        if (permissions.Contains("admin_local"))
        {
            exludeApp = true;
        }
        if (!permissions.Contains("list_org_app"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _organizationApplicationService.GetByOrganizationIdAsync(pageNumber, pageSize, organizationId, exludeApp, search);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("Organization")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<OrganizationApplicationVm>>>> GetByOrganizationId(int? pageNumber,
         int? pageSize, string? search)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool? exludeApp = null;
        if (permissions.Contains("admin_local"))
        {
             exludeApp = true;
        }
        if (!permissions.Contains("list_org_app"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var orgId = GetOrganizationIdFromToken();
        if (orgId == 0)
            return BadRequest(new Response<object>(null, "No se pudo obtener la organización desde el token."));
        var response = await _organizationApplicationService.GetByOrganizationIdAsync(pageNumber, pageSize, orgId, exludeApp, search);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
}
