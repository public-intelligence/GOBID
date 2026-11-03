using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Policies;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Policies;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class PolicyController : OwnBaseController
{
    private readonly IPolicyService _policyService;

    public PolicyController(IPolicyService policyService, IAuthService authService)
        : base(authService)
    {
        _policyService = policyService;
    }

    [HttpGet("organization/{organizationId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<PolicyVm>>>> ListByOrganization(int organizationId, [FromQuery] bool? isActive)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "list_policy"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _policyService.ListByOrganizationAsync(organizationId, isActive);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("{policyId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<PolicyVm>>> GetById(Guid policyId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "list_policy"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _policyService.GetByIdAsync(policyId);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [HttpPut]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> Upsert([FromBody] PolicyUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "edit_policy"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var userId = GetUserIdFromToken();
        if (userId == null) return Unauthorized("No se pudo obtener el ID del usuario desde el token.");

        if (dto.PolicyId == null || dto.PolicyId == Guid.Empty)
            dto.CreatedBy = userId;
        else
            dto.UpdatedBy = userId;

        var response = await _policyService.UpsertAsync(dto);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{policyId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> Delete(Guid policyId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "delete_policy"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _policyService.DeleteAsync(policyId);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }
}
