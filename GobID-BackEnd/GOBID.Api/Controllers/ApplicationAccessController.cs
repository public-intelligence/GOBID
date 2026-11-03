using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.ApplicationAccess;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.ApplicationAccess;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class ApplicationAccessController : OwnBaseController
{
    private readonly IApplicationAccessService _applicationAccessService;

    public ApplicationAccessController(IApplicationAccessService applicationAccessService, IAuthService authService) : base(authService)
    {
        _applicationAccessService = applicationAccessService;
    }

    [HttpPut()]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Upsert(ApplicationAccessUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _applicationAccessService.UpsertAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet()]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<ApplicationAccessVm>>>> GetAccessTypes([FromQuery] ApplicationAccessFilterDto? filter)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _applicationAccessService.GetAccessTypes( 1, 50, filter);
        if (!response.Succeeded)
            return NotFound(response);

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
        var response = await _applicationAccessService.DeleteAsync(id);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
}
