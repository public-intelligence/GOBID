using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Roles;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Roles;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;


[Route("api/v{version:apiVersion}/[controller]")]
public class RoleController : OwnBaseController
{
   private readonly IRoleService _roleService;
    public RoleController(IRoleService roleService, IAuthService authService) : base(authService)
    {
        _roleService = roleService;
    }

    [HttpGet("{roleId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<RoleVm>>> GetRole(
        int pageNumber, int pageSize, int roleId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_applications"))
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));
        var response = await _roleService.GetByIdAsync(roleId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
    [HttpGet()]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<RoleVm>>>> ListRoles(
        int? pageNumber, int? pageSize, [FromQuery] RoleFilterDto? filter)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool hasM2M = permissions.Contains("access:M2M");
        bool hasListApplications = permissions.Contains("list_applications");

        if (hasM2M && !hasListApplications)
            return StatusCode(403, new Response<object>(null!, "El permiso 'access:M2M' requiere también el permiso 'list_applications'."));

        if (!hasM2M && !hasListApplications)
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));

        var response = await _roleService.ListAsync(pageNumber, pageSize, filter ?? new RoleFilterDto());
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("by-structure/{structureAreaId:guid}")]
    [ProducesResponseType(typeof(PagedResponse<RoleVm>), 200)]
    public async Task<ActionResult<PagedResponse<RoleVm>>> ListByStructureArea(
        Guid structureAreaId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] string? search,
        [FromQuery] int? organizationApplicationId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool hasM2M = permissions.Contains("access:M2M");
        bool hasListApplications = permissions.Contains("list_applications");

        if (hasM2M && !hasListApplications)
            return StatusCode(403, new Response<object>(null!, "El permiso 'access:M2M' requiere también el permiso 'list_applications'."));

        if (!hasM2M && !hasListApplications)
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));

        var organizationId = GetOrganizationIdFromToken();
        if (organizationId == 0)
            return Unauthorized("No se pudo obtener la organización desde el token.");

        var orgAppId = organizationApplicationId ?? GetOrganizationApplicationIdFromToken();
        if (orgAppId == null)
            return BadRequest(new Response<object>(null!, "No se pudo determinar la aplicación de organización."));

        var response = await _roleService.ListByStructureAreaAsync(
            organizationId, orgAppId.Value, structureAreaId, pageNumber, pageSize, search);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPut()]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<RoleVm>>> Upsert(RoleUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_applications"))
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));
        var user = GetUserIdFromToken();
        if (user == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        dto.UpdatedBy = user.Value;
        dto.CreatedBy = user.Value;
        var response = await _roleService.UpsertAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{roleId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Delete(int roleId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("delete_applications"))
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));
        var response = await _roleService.DeleteAsync(roleId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("by-position/{positionId:guid}")]
    [ProducesResponseType(typeof(Response<int>), 200)]
    public async Task<ActionResult<Response<int>>> DeleteByPosition(Guid positionId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("delete_applications"))
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));
        var response = await _roleService.DeleteByPositionAsync(positionId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

}
