using IdentityHub.Domain.DTOs.Organizations;
using IdentityHub.Domain.DTOs.Pagination;
using IdentityHub.Domain.Enums;
using IdentityHub.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.Controllers;

[ApiController]
[Route("api/[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class OrganizationController : ControllerBase
{
   private readonly IOrganizationServices _organizationServices;
   private readonly IPaginationService _paginationService;

    public OrganizationController(IOrganizationServices organizationServices, IPaginationService paginationService)
   {
       _organizationServices = organizationServices;
       _paginationService = paginationService;

    }

    
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LegalEntityDTO>> GetById(Guid id)
    {
        var result = await _organizationServices.GetByIdAsync(id);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        var org = result.Value;
        return Ok(org);
    }


    [HttpGet("rnc/{identificationNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LegalEntityDTO>> GetByIdentificationAsync(string identificationNumber)
    {
        var result = await _organizationServices.GetByIdentificationNumberAsync(identificationNumber);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        var org = result.Value;
        return Ok(org);
    }
    [HttpGet()]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponseDTO<LegalEntityDTO>>> GetAll([FromQuery] PaginationFilterDTO pagination, [FromQuery] string? search = null,
        [FromQuery] EnumStatus? status = null)
    {
        var result = await _organizationServices.GetAllAsync(pagination, HttpContext, search, status);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        var pagedData = result.Value;

        _paginationService.SetPaginationHeaders(Response, pagedData);

        return Ok(pagedData.Value);
    }
     [HttpPost()]
     [ProducesResponseType(StatusCodes.Status201Created)]
     [ProducesResponseType(StatusCodes.Status400BadRequest)]
     [ProducesResponseType(StatusCodes.Status500InternalServerError)]
     public async Task<ActionResult<LegalEntityDTO>> Create(CreateLegalEntityDTO organizationDto)
     {
         var result = await _organizationServices.CreateAsync(organizationDto);
         if (!result.IsSuccess)
         {
             return StatusCode((int)result.StatusCode, result.Error);
         }
        var org = result.Value;
        return CreatedAtAction(nameof(GetById), new { id = org!.Id }, org);
     }
     [HttpPatch("{id}")]
     [ProducesResponseType(StatusCodes.Status200OK)]
     [ProducesResponseType(StatusCodes.Status400BadRequest)]
     [ProducesResponseType(StatusCodes.Status404NotFound)]
     [ProducesResponseType(StatusCodes.Status500InternalServerError)]
     public async Task<ActionResult<LegalEntityDTO>> Update(Guid id, UpdateOrganizationDTO organizationDto)
     {
         var result = await _organizationServices.UpdateAsync(id, organizationDto);
         if (!result.IsSuccess)
         {
             return StatusCode((int)result.StatusCode, result.Error);
         }
        var org = result.Value;
        return Ok(org);
    }
}
