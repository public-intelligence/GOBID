using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Application;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Application;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class ApplicationController : OwnBaseController
{
    private readonly IApplicationService _applicationService;
    public ApplicationController(IApplicationService applicationService, IAuthService authService) : base(authService)
    {
        _applicationService = applicationService;
    }
    [HttpGet()]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<ApplicationVm>>> GetApplications(
        int? pageNumber, int? pageSize, [FromQuery] ApplicationFilterDto? filter)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _applicationService.ListAsync(pageNumber, pageSize, filter);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPut()]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<ApplicationUpsertResponseVm>>> Upsert(ApplicationUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        dto.UpdatedBy = userId.Value;
        dto.CreatedBy = userId.Value;
        var response = await _applicationService.UpsertAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{appId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Delete(Guid appId, [FromQuery] ApplicationDeleteDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("delete_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        dto.UpdatedBy = userId.Value;
        var response = await _applicationService.DeleteAsync(appId, dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("{appId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<ApplicationVm>>> GetApplication(Guid appId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _applicationService.GetAsync(1, 1, appId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("/api/v{version:apiVersion}/public/application/{appId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<ApplicationVm>>> GetApplicationAsync(Guid appId)
    {
        var response = await _applicationService.GetAsync(1, 1, appId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("{appId}/duplicate")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> Duplicate(Guid appId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        var response = await _applicationService.DuplicateAsync(appId, userId.Value);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("generate-string/{appId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> GetGenerateString(Guid appId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _applicationService.GetGeneratestring(appId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
}
