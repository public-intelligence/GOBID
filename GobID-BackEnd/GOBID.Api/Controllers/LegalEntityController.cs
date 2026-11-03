using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.EntityOrganization;
using GOBID.Application.Enums;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/identityHub/[controller]")]
[ApiController]
public class LegalEntityController : OwnBaseController
{
    private readonly ILegalEntityService _entityOrganizationService;

    public LegalEntityController(ILegalEntityService entityOrganizationService, IAuthService authService) : base(authService)
    {
        _entityOrganizationService = entityOrganizationService;
    }


    /// <summary>
    /// Crea una persona en la API externa
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<Response<LegalEntityDTO>>> CreateAsync([FromBody] CreateLegalEntityDTO createOrg)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("edit_le"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _entityOrganizationService.CreateOrganizationAsync(createOrg);

        if (!response.Succeeded)
            return BadRequest(response);

        return Created($"api/Person/{response.Data!.Id}", response);
    }

    /// <summary>
    /// Obtiene una persona por ID desde la API externa
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Response<LegalEntityDTO>>> GetAsyncById(Guid id)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("list_le"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _entityOrganizationService.GetOrganizationByIdAsync(id);

        if (!response.Succeeded)
            return NotFound(response);

        return Ok(response);
    }

    [HttpGet("identificationNumber/{identificationNumber}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Response<LegalEntityDTO>>> GetAsyncByIdentificationNumber(string identificationNumber)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("list_le"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _entityOrganizationService.GetOrganizationByIdentificationNumberAsync(identificationNumber);

        if (!response.Succeeded)
            return NotFound(response);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("/api/v{version:apiVersion}/public/identityHub/LegalEntity/identificationNumber/{identificationNumber}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Response<LegalEntityDTO>>> GetAsyncByIdentificationNumberPublic(string identificationNumber)
    {
        var response = await _entityOrganizationService.GetOrganizationByIdentificationNumberAsync(identificationNumber);

        if (!response.Succeeded)
            return NotFound(response);

        return Ok(response);
    }

    /// <summary>
    /// Obtiene las personas desde la API externa
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<Response<LegalEntityDTO>>> GetAsync([FromQuery] int? pageNumber, [FromQuery] int? pageSize, [FromQuery] string? search, [FromQuery] EnumStatus? status)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("list_le"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _entityOrganizationService.GetOrganizationsAsync(pageNumber, pageSize, search, status);

        if (!response.Succeeded)
            return NotFound(response);

        return Ok(response);
    }

    /// <summary>
    /// Obtiene las personas desde la API externa
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<Response<LegalEntityDTO>>> UpdateAsync(Guid id, UpdateLegalEntityDTO updateOrg)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("edit_le"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _entityOrganizationService.UpdateOrganizationAsync(id, updateOrg);

        if (!response.Succeeded)
            return NotFound(response);

        return Ok(response);
    }

}
