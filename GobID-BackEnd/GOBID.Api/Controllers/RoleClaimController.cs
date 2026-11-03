using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Roleclaim;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.RoleClaim;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class RoleClaimController : OwnBaseController
{
    private readonly IRoleClaimService _roleClaimService;

    public RoleClaimController(IRoleClaimService roleClaimService, IAuthService authService) : base(authService)
    {
        _roleClaimService = roleClaimService;
    }

    [HttpPut()]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> UpsertRoleClaimAsync(RoleClaimUpsertDto[] dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        foreach (var item in dto)
        {
            item.UpdatedBy = userId.Value;
            item.CreatedBy = userId.Value;
        }
        var response = await _roleClaimService.UpsertAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet()]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<RoleClaimVm>>>> GetRoleClaimsAsync(int? pageNumber, int? pageSize, int? roleId, bool? isActive)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _roleClaimService.ListAsync(pageNumber, pageSize, roleId, isActive);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> DeleteRoleClaimAsync(int id)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("delete_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        var updatedBy = userId.Value;
        var response = await _roleClaimService.DeleteAsync(id, updatedBy);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<RoleClaimVm>>> GetRoleClaimAsync(int id)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_applications"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _roleClaimService.GetAsync(id);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }
}