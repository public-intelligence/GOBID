using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Personnel;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Personnel;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class PersonnelController : OwnBaseController
{
    private readonly IPersonnelService _personnelService;

    public PersonnelController(IPersonnelService personnelService, IAuthService authService)
        : base(authService)
    {
        _personnelService = personnelService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<PersonnelVm>>> GetAsync(Guid id)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || permissions.Length == 0)
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool hasM2M = permissions.Contains("access:M2M");
        bool hasListUser = permissions.Contains("list_personnel");

        if (hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "El permiso 'access:M2M' requiere también el permiso 'list_user'."));

        if (!hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));

        var response = await _personnelService.GetAsync(id);
        return response.Succeeded ? Ok(response) : NotFound(response);
    }

    [HttpGet("organization/{organizationId:int}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<PersonnelVm>>> ListAsync(
        int organizationId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] bool? isActive,
        [FromQuery] Guid? positionId,
        [FromQuery] int? roleId,
        [FromQuery] string? searchTerm)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || permissions.Length == 0)
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool hasM2M = permissions.Contains("access:M2M");
        bool hasListUser = permissions.Contains("list_personnel");

        if (hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "El permiso 'access:M2M' requiere también el permiso 'list_user'."));

        if (!hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));

        var response = await _personnelService.ListAsync(organizationId, pageNumber, pageSize, isActive, positionId,roleId, searchTerm);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPut]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<PersonnelVm>>> UpsertAsync([FromBody] PersonnelUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "edit_personnel"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");

        dto.CreatedBy = userId.Value;
        dto.UpdatedBy = userId.Value;

        var response = await _personnelService.UpsertAsync(dto);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> DeleteAsync(Guid id)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || permissions.Length == 0)
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "delete_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");

        var response = await _personnelService.DeleteAsync(id, userId.Value);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Endpoint M2M: dado un roleId, retorna el personal del nivel supervisorio
    /// más cercano que tenga personal activo en la jerarquía organizacional.
    /// </summary>
    [HttpGet("organization/{organizationId}/role/{roleId}/supervisors")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<PersonnelSupervisorResultVm>>> GetSupervisorPersonnelAsync(
        [FromRoute] int organizationId,
        [FromRoute] int roleId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || permissions.Length == 0)
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool hasM2M = permissions.Contains("access:M2M");
        bool hasListUser = permissions.Contains("list_personnel");

        if (hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "El permiso 'access:M2M' requiere también el permiso 'list_user'."));

        if (!hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));

        if (roleId <= 0)
            return BadRequest(new Response<object>(null, "Debe proporcionar un roleId válido."));

        var response = await _personnelService.GetSupervisorPersonnelAsync(organizationId, roleId);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpGet("organization/{organizationId}/role/{roleId}/direct-manager")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<PersonnelSupervisorResultVm>>> GetDirectManagerPersonnelAsync(
        [FromRoute] int organizationId,
        [FromRoute] int roleId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || permissions.Length == 0)
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool hasM2M = permissions.Contains("access:M2M");
        bool hasListUser = permissions.Contains("list_personnel");

        if (hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "El permiso 'access:M2M' requiere también el permiso 'list_personnel'."));

        if (!hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));

        if (roleId <= 0)
            return BadRequest(new Response<object>(null, "Debe proporcionar un roleId válido."));

        var response = await _personnelService.GetDirectManagerPersonnelAsync(organizationId, roleId);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [ProducesResponseType(200)]
    [HttpGet("organization/{organizationId}/role/{roleId}/subordinates")]
    public async Task<ActionResult<Response<List<PersonnelVm>>>> GetSubordinatePersonnelM2M(
        int organizationId,
        int roleId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || permissions.Length == 0)
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool hasM2M = permissions.Contains("access:M2M");
        bool hasListUser = permissions.Contains("list_personnel");

        if (hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "El permiso 'access:M2M' requiere también el permiso 'list_personnel'."));

        if (!hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));

        if (roleId <= 0)
            return BadRequest(new Response<object>(null, "Debe proporcionar un roleId válido."));

        var response = await _personnelService.GetSubordinatePersonnelAsync(organizationId, roleId);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [ProducesResponseType(200)]
    [HttpGet("organization/{organizationId}/person/{personId}/subordinates")]
    public async Task<ActionResult<Response<List<Guid>>>> GetSubordinateUserIdsByPersonIdM2M(
        int organizationId,
        Guid personId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || permissions.Length == 0)
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool hasM2M = permissions.Contains("access:M2M");
        bool hasListUser = permissions.Contains("list_personnel");

        if (hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "El permiso 'access:M2M' requiere también el permiso 'list_personnel'."));

        if (!hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));

        var response = await _personnelService.GetSubordinateUserIdsByPersonIdAsync(organizationId, personId);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Endpoint M2M: dada una estructura organizacional (departamento) de PDS, retorna los correos
    /// de los usuarios (tabla User) cuyo personal activo ocupa alguna posición de esa estructura.
    /// Pensado para que sistemas consumidores (ej. SGSC) puedan notificar por departamento.
    /// </summary>
    [ProducesResponseType(200)]
    [HttpGet("organization/{organizationId}/structure/{structureId}/users/emails")]
    public async Task<ActionResult<Response<List<UserContactDto>>>> GetUserEmailsByOrganizationStructureAsync(
        int organizationId,
        Guid structureId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || permissions.Length == 0)
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool hasM2M = permissions.Contains("access:M2M");
        bool hasListUser = permissions.Contains("list_personnel");

        if (hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "El permiso 'access:M2M' requiere también el permiso 'list_personnel'."));

        if (!hasM2M && !hasListUser)
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));

        var response = await _personnelService.GetUserEmailsByOrganizationStructureAsync(organizationId, structureId);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

}
