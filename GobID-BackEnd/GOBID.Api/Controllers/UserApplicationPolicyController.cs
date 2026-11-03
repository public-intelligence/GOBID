using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.UserApplicationPolicy;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.UserApplicationPolicy;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class UserApplicationPolicyController : OwnBaseController
{
    private readonly IUserApplicationPolicyService _service;

    public UserApplicationPolicyController(IUserApplicationPolicyService service, IAuthService authService)
        : base(authService)
    {
        _service = service;
    }

    [HttpGet("{userApplicationId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<UserApplicationPolicyVm>>>> ListByUserApplication(
        Guid userApplicationId, [FromQuery] bool? isActive)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "list_policy"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _service.ListByUserApplicationAsync(userApplicationId, isActive);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("assign")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> Assign([FromBody] UserApplicationPolicyUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "edit_policy"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var userId = GetUserIdFromToken();
        if (userId == null) return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        dto.CreatedBy = userId;

        var response = await _service.AssignAsync(dto);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{userApplicationPolicyId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> Remove(Guid userApplicationPolicyId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "delete_policy"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var userId = GetUserIdFromToken();
        if (userId == null) return Unauthorized("No se pudo obtener el ID del usuario desde el token.");

        var response = await _service.RemoveAsync(userApplicationPolicyId, userId.Value);
        if (!response.Succeeded) return BadRequest(response);
        return Ok(response);
    }
}
