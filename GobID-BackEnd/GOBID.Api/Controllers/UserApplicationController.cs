using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.UserApplication;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.UserApplication;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class UserApplicationController : OwnBaseController
{
    private readonly IUserApplicationService _userApplicationService;

    public UserApplicationController(IUserApplicationService userApplicationService, IAuthService authService
    ) : base(authService)
    {
        _userApplicationService = userApplicationService;
    }

    [HttpGet()]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<UserApplicationVm>>>> ListUserApplications(
        int? pageNumber, int? pageSize, [FromQuery] UserApplicationFilterDto filter)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_user_app"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _userApplicationService.ListAsync(pageNumber, pageSize, filter);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("{userApplicationId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<UserApplicationVm>>> GetUserApplicationById(Guid userApplicationId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_user_app"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _userApplicationService.GetAsync(1, 50, userApplicationId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPut()]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Upsert(UserApplicationUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_user_app"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("User ID not found in token.");
        dto.UpdatedBy = userId.Value;
        var response = await _userApplicationService.UpsertAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{userApplicationId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Delete(Guid userApplicationId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("delete_user_app"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("User ID not found in token.");
        var updatedBy = userId.Value;
        var response = await _userApplicationService.DeleteAsync(userApplicationId, updatedBy);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
}
