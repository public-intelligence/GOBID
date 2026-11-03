using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.AccessType;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class AccessTypeController : OwnBaseController
{
    private readonly IAccessTypeService _accessTypeService;
    public AccessTypeController(IAccessTypeService accessTypeService, IAuthService authService
    ) : base(authService)
    {
        _accessTypeService = accessTypeService;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<AccessTypeDto>>> GetAccessType(
        [FromQuery] int pageNumber, int pageSize, int id)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _accessTypeService.GetAccessType(pageNumber = 1, pageSize = 50, id);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPut()]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Upsert(AccessTypeUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _accessTypeService.UpsertAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Delete(int id)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("delete_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _accessTypeService.DeleteAsync(id);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
    [HttpGet()]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<AccessTypeDto>>>> ListAccessTypes( int pageNumber, int pageSize, [FromQuery] AccessTypeFilterDto? filter)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _accessTypeService.ListAccessTypes(pageNumber = 1, pageSize = 50, filter);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
}