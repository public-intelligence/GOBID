using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.ApplicationRole;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.ApplicationRole;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

/// <summary>
/// Gestión de roles asignados a credenciales M2M (service accounts).
/// Los roles determinan los permisos que se incluyen en el JWT M2M al autenticar.
/// </summary>
[Route("api/v{version:apiVersion}/credentials/{appCredentialId}/roles")]
public class ApplicationRoleController : OwnBaseController
{
    private readonly IApplicationRoleService _applicationRoleService;

    public ApplicationRoleController(
        IAuthService authService,
        IApplicationRoleService applicationRoleService) : base(authService)
    {
        _applicationRoleService = applicationRoleService;
    }

    /// <summary>
    /// Obtiene un rol de aplicación por ID.
    /// </summary>
    [HttpGet("{applicationRoleId:int}")]
    [ProducesResponseType(typeof(Response<ApplicationRoleVm>), 200)]
    public async Task<ActionResult<Response<ApplicationRoleVm>>> GetById(
        [FromRoute] Guid appCredentialId,
        [FromRoute] int applicationRoleId)
    {
        var response = await _applicationRoleService.GetByIdAsync(applicationRoleId);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Response<List<ApplicationRoleVm>>), 200)]
    public async Task<ActionResult<Response<List<ApplicationRoleVm>>>> List(
        [FromRoute] Guid appCredentialId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] int? organizationApplicationId,
        [FromQuery] int? roleId,
        [FromQuery] bool? isActive)
    {
        var response = await _applicationRoleService.ListAsync(
            pageNumber, pageSize,
            appCredentialId, organizationApplicationId,
            roleId, isActive);

        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    /// <summary>
    /// Crea o actualiza un rol de aplicación.
    /// Si <c>ApplicationRoleId</c> está presente actualiza, de lo contrario crea.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(Response<ApplicationRoleVm>), 200)]
    public async Task<ActionResult<Response<ApplicationRoleVm>>> Upsert(
        [FromRoute] Guid appCredentialId,
        [FromBody] ApplicationRoleUpsertDto dto)
    {
        var updatedBy = GetUserIdFromToken();
        if (updatedBy == null)
            return Unauthorized("No se pudo identificar al usuario autenticado.");

        dto.AppCredentialId = appCredentialId;

        var response = await _applicationRoleService.UpsertAsync(dto, updatedBy.Value);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    /// <summary>
    /// Desactiva (soft-delete) un rol de aplicación.
    /// </summary>
    [HttpDelete("{applicationRoleId:int}")]
    [ProducesResponseType(typeof(Response<string>), 200)]
    public async Task<ActionResult<Response<string>>> Delete(
        [FromRoute] Guid appCredentialId,
        [FromRoute] int applicationRoleId)
    {
        var updatedBy = GetUserIdFromToken();
        if (updatedBy == null)
            return Unauthorized("No se pudo identificar al usuario autenticado.");

        var response = await _applicationRoleService.DeleteAsync(applicationRoleId, updatedBy.Value);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
}
