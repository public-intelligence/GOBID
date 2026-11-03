using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.UserRole;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.UserRole;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class UserRoleController : OwnBaseController
{
    private readonly IUserRoleService _userRoleService;
    public UserRoleController(IUserRoleService userRoleService, IAuthService authService
    ) : base(authService)
    {
        _userRoleService = userRoleService;
    }

    [HttpGet("{userRoleId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<UserRoleVm>>> GetUserRoles(int userRoleId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _userRoleService.GetByIdAsync(userRoleId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet()]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<UserRoleVm>>>> ListUserRoles(int? pageNumber, int? pageSize, int? userRoleId, Guid? userApp, int? roleId, bool? isActive)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _userRoleService.ListAsync(pageNumber, pageSize, userRoleId, userApp, roleId, isActive);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPut()]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Upsert(UserRoleUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var user = GetUserIdFromToken();
        if (user == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        dto.UpdatedBy = user.Value;
        var response = await _userRoleService.UpsertAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpDelete("{userRoleId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Delete(int userRoleId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("delete_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var user = GetUserIdFromToken();
        if (user == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        var updatedBy = user.Value;
        var response = await _userRoleService.DeleteAsync(userRoleId, updatedBy);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

}