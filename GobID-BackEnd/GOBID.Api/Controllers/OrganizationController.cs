using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Organization;
using GOBID.Application.DTOs.OrganizationContact;
using GOBID.Application.DTOs.Pds;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Organization;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
public class OrganizationController : OwnBaseController
{
    private readonly IOrganizationContactService _contactService;
    private readonly IOrganizationService _organizationService;
    private readonly IPdsApiService _pdsApiService;
    public OrganizationController(IOrganizationService organizationService, IOrganizationContactService contactService, IAuthService authService, IPdsApiService pdsApiService) : base(authService)
    {
        _organizationService = organizationService;
        _contactService = contactService;
        _pdsApiService = pdsApiService;
    }

    [HttpGet()]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponse<OrganizationVm>>> GetOrganization(
        int? pageNumber,
        int? pageSize,
        [FromQuery] OrganizationFilterDto? filter)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        int? organizationId = null;
        if (permissions.Contains("admin_local"))
        {
            organizationId = GetOrganizationIdFromToken();
        }
        if (!permissions.Contains("list_org"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _organizationService.GetOrganizationsAsync(pageNumber, pageSize, filter, organizationId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPut()]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Upsert(OrganizationUpsertDto dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("edit_organizations"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _organizationService.UpsertAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{orgId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> Delete(int orgId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("delete_organizations"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized("No se pudo obtener el ID del usuario desde el token.");
        var deletedBy = userId.Value;
        var response = await _organizationService.DeleteAsync(orgId, deletedBy);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("{orgId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<OrganizationDto>>> Get(int orgId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("list_org"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _organizationService.GetAsync(1, 50, orgId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }


    [HttpGet("contacts/{orgId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<Response<List<OrganizationContactDto>>>> GetContacts(int orgId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("list_org"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _contactService.ListAsync(orgId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPut("contacts")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> UpsertContact(OrganizationContactUpsertDto[] dto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("edit_organizations"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _contactService.UpsertAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("contacts/{contactId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult<Response<string>>> DeleteContact(int contactId)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!permissions.Contains("delete_organizations"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));
        var response = await _contactService.DeleteAsync(contactId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("{orgId}/locality")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Response<PdsLocalidadDto>>> GetLocalityAsync(int orgId, [FromQuery] PdsLocalidadFilterDto pdsLocalidadFilterDto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");
        if (!HasPermissionOrSuperAdmin(permissions, "edit_personnel"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _pdsApiService.GetLocalidadesAsync(orgId, pdsLocalidadFilterDto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }
}
