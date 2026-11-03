using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Policies;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Policies;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class PolicyTypeController : OwnBaseController
{
    private readonly IPolicyTypeService _policyTypeService;

    public PolicyTypeController(IPolicyTypeService policyTypeService, IAuthService authService)
        : base(authService)
    {
        _policyTypeService = policyTypeService;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<PolicyTypeVm>>>> List([FromQuery] bool? isActive)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "list_policy"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _policyTypeService.ListAsync(isActive);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("{policyTypeId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<PolicyTypeVm>>> GetById(int policyTypeId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "list_policy"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _policyTypeService.GetByIdAsync(policyTypeId);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [HttpPut]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> Upsert([FromBody] PolicyTypeUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "edit_policy"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var userId = GetUserIdFromToken();
        if (userId == null) return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        dto.CreatedBy = userId;

        var response = await _policyTypeService.UpsertAsync(dto);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{policyTypeId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> Delete(int policyTypeId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "delete_policy"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _policyTypeService.DeleteAsync(policyTypeId);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }
}
