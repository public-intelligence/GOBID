using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Auth;
using GOBID.Application.DTOs.Personnel;
using GOBID.Application.DTOs.User;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Security;
using GOBID.Application.ViewModels.User;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;


[Route("api/v{version:apiVersion}/[controller]")]
public class UserController : OwnBaseController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService, IAuthService authService
    ) : base(authService)
    {
        _userService = userService;
    }

    [HttpPut()]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Upsert(UserUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        Guid? applicationid = null;
        if (permissions.Contains("admin_local"))
        {
            applicationid = GetApplicationIdFromToken();
            if (applicationid == null)
                return Unauthorized("No se pudo obtener el ID de la applicacion desde el token.");
            
        }
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        dto.UpdatedBy = userId.Value;
        var response = await _userService.Upsert(dto, applicationid);
        if (!response.Succeeded)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPut("status")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> UpdateUser(UserStatusUpdateDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        dto.UpdatedBy = userId.Value;
        var response = await _userService.UpdateUserStatusAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{userId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Delete(Guid userId, string? observation)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("delete_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var user = GetUserIdFromToken();
        if (user == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        var deleteBy = user.Value;
        var response = await _userService.Delete(userId, deleteBy, observation);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
    [HttpGet()]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<UserVm>>> GetUsers(int? pageNumber, int? pageSize, [FromQuery] UserFilterDto? filter)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        int? organizationId = null;
        if (permissions.Contains("admin_local"))
        {
            organizationId = GetOrganizationIdFromToken();
        }
        if (!permissions.Contains("list_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        
        var response = await _userService.ListAsync(pageNumber, pageSize, filter, organizationId);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<UserVm>>> GetByIdAsync(Guid userId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        int? organizationId = null;
        if (permissions.Contains("admin_local"))
        {
            organizationId = GetOrganizationIdFromToken();
        }
        var response = await _userService.GetByIdAsync(userId, organizationId);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpGet("personnel/{identificationNumber}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<PersonnelDto>>> GetPersonnelByIdentificationNumber(string identificationNumber)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _userService.GetPersonnelByIdentificationNumber(identificationNumber);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpGet("personnel/id/{personnelId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<PersonnelDto>>> GetPersonnelByIdAsync(Guid personnelId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _userService.GetPersonnelByIdAsync(personnelId);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpGet("count")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<UserVm>>> GetCount(int? pageNumber, int? pageSize, [FromQuery] UserFilterDto? filter)
    {
        var response = await _userService.ListCountAsync(pageNumber, pageSize, filter);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpPut("change-password/{userId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> ChangePassword(Guid userId, ChangePasswordDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _userService.ChangePasswordAsync(userId, dto);
        if (response == null || !response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("personnel-info/{personnelId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<PersonnelInfoDto>>> GetPersonnelInfoByIdAsync(Guid personnelId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _userService.GetPersonnelInfoByIdAsync(personnelId);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpPut("user-organization")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> UpsertUserOrganization(UserOrganizationUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_user"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _userService.UpsertUserOrganization(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("organization-application/{organizationApplicationId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<UserWithPersonnelDto>>> GetUsersByOrganizationApplication(
        int organizationApplicationId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var response = await _userService.ListUsersByOrganizationApplicationAsync(organizationApplicationId, pageNumber, pageSize);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpGet("organization-application/{organizationApplicationId}/position")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<UserWithPersonnelDto>>> GetUsersByOrganizationApplicationWithPosition(
        int organizationApplicationId,
        [FromQuery] string? searchposition,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool hasM2M = permissions.Contains("access:M2M");
        bool hasList_user = permissions.Contains("list_user");

        if (hasM2M && !hasList_user)
            return StatusCode(403, new Response<object>(null!, "El permiso 'access:M2M' requiere también el permiso 'list_user'."));

        if (!hasM2M && !hasList_user)
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));
        var response = await _userService.GetUsersByOrganizationApplicationWithPositionAsync(organizationApplicationId, searchposition, pageNumber, pageSize);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("register-public")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<string>>> RegisterPublic([FromBody] PublicRegisterDto dto)
    {
        var response = await _userService.RegisterPublicAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    
    [HttpGet("organization/{organizationId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<UserVm>>> GetUsersByOrganization(
        int organizationId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] UserOrganizationFilterDto? filter)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        bool hasM2M = permissions.Contains("access:M2M");
        bool hasList_user = permissions.Contains("list_user");

        if (hasM2M && !hasList_user)
            return StatusCode(403, new Response<object>(null!, "El permiso 'access:M2M' requiere también el permiso 'list_user'."));

        if (!hasM2M && !hasList_user)
            return StatusCode(403, new Response<object>(null!, "No tienes permiso para realizar esta acción."));

        // G1: list_user no da acceso a cualquier organización. Usuario: solo la de su token. Cuenta de
        // servicio: también las organizaciones donde su aplicación está instalada. super_admin: todas.
        var tokenOrganizationId = GetOrganizationIdFromToken();
        var applicationInstalled = false;
        if (tokenOrganizationId != organizationId
            && string.Equals(User.FindFirst("is_service_account")?.Value, "true", StringComparison.OrdinalIgnoreCase)
            && GetApplicationIdFromToken() is { } applicationId)
        {
            applicationInstalled = await _userService.IsApplicationInOrganizationAsync(applicationId, organizationId);
        }

        if (!OrganizationScope.CanAccessOrganization(tokenOrganizationId, organizationId, permissions, applicationInstalled))
            return StatusCode(403, new Response<object>(null!, "No puede consultar los usuarios de otra organización."));

        var response = await _userService.ListUsersByOrganizationAsync(organizationId, pageNumber, pageSize, filter);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpGet("email")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<UserVm>>> GetByEmailAsync([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new Response<UserVm>("El correo electrónico es requerido", false));

        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        bool hasM2M = permissions.Contains("access:M2M");
        bool hasList_user = permissions.Contains("list_user");

        if (hasM2M && !hasList_user)
            return base.StatusCode(403, new Response<object>((object)null!, "El permiso 'access:M2M' requiere también el permiso 'list_user'."));

        if (!hasM2M && !hasList_user)
            return base.StatusCode(403, new Response<object>((object)null!, "No tienes permiso para realizar esta acción."));

        var response = await _userService.GetByEmailAsync(email);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }

    [HttpGet("info/{userId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<UserVm>>> GetByIdInfoAsync([FromRoute] Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest(new Response<UserVm>("El ID de usuario es requerido", false));


        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        bool hasM2M = permissions.Contains("access:M2M");
        bool hasList_user = permissions.Contains("list_user");

        if (hasM2M && !hasList_user)
            return base.StatusCode(403, new Response<object>((object)null!, "El permiso 'access:M2M' requiere también el permiso 'list_user'."));

        if (!hasM2M && !hasList_user)
            return base.StatusCode(403, new Response<object>((object)null!, "No tienes permiso para realizar esta acción."));
        var organizationId = GetOrganizationIdFromToken();

        var response = await _userService.GetByIdInfoAsync(userId, organizationId);
        if (!response.Succeeded)
            return NotFound(response);
        return Ok(response);
    }
}