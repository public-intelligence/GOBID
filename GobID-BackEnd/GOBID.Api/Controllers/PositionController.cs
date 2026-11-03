using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Pds;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class PositionController : OwnBaseController
{
    private readonly IPdsApiService _pdsApiService;

    public PositionController(IPdsApiService pdsApiService, IAuthService authService) : base(authService)
    {
        _pdsApiService = pdsApiService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<PdsPositionDto>>> GetPositionAsync(Guid id)
    {
        var organizationId = GetOrganizationIdFromToken();
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "list_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _pdsApiService.GetPositionAsync(organizationId, id);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<PdsPositionDto>>> GetPositionsAsync(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var organizationId =  GetOrganizationIdFromToken();
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || permissions.Length == 0)
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "list_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _pdsApiService.GetPositionsAsync(organizationId, pageNumber, pageSize);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("search")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<PdsPositionDto>>> SearchPositionsAsync(
        [FromQuery] string? q,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var organizationId = GetOrganizationIdFromToken();
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "list_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _pdsApiService.SearchPositionsAsync(organizationId, q, pageNumber, pageSize);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
}
