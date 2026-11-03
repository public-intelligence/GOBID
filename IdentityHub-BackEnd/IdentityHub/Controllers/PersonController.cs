using IdentityHub.Domain.DTOs.Pagination;
using IdentityHub.Domain.DTOs.Persons;
using IdentityHub.Domain.Enums;
using IdentityHub.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.Controllers;

[Route("api/[controller]")]
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
public class PersonController : ControllerBase
{
    private readonly IPersonServices _personService;
    private readonly IPaginationService _paginationService;

    public PersonController(

        IPersonServices personService, 
        IPaginationService paginationService
    )
    {
        _personService = personService;
        _paginationService = paginationService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] PaginationFilterDTO pagination,
        [FromQuery] string? search = null,
        [FromQuery] EnumStatus? status = null)
    {
        var result = await _personService.GetAllAsync(pagination, HttpContext, search, status);

        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        var pagedData = result.Value;

        _paginationService.SetPaginationHeaders(Response, pagedData);

        return Ok(pagedData.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PersonDTO>> GetAsyncById(Guid id)
    {
        var result = await _personService.GetAsyncById(id);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        var person = result.Value;
        return Ok(person);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAsync(CreatePersonDTO createPersonDto)
    {
        var result = await _personService.CreateAsync(createPersonDto);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        var person = result.Value;
        return CreatedAtAction(nameof(GetAsyncById), new { id = person!.Id }, person);
    }

    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> UpdateAsync(Guid id, UpdatePersonDTO updatePersonDto)
    {
        var result = await _personService.UpdateAsync(id, updatePersonDto);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        var person = result.Value;
        return Ok(person);
    }

    [HttpPatch("{id}/profile-photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PersonDTO>> UpdateProfilePhotoAsync(Guid id, UpdateProfilePhotoDTO updateProfilePhotoDto)
    {
        var result = await _personService.UpdateProfilePhotoAsync(id, updateProfilePhotoDto.ProfilePhotoId);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        var person = result.Value;
        return Ok(person);
    }

    [HttpGet("cedula/{cedula}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PersonDTO>> GetAsyncByCedula(string cedula)
    {
        var result = await _personService.GetAsyncByCedula(cedula);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        var person = result.Value;
        return Ok(person);
    }

    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PersonDTO>>> GetByIdsAsync([FromBody] IEnumerable<Guid> ids)
    {
        var result = await _personService.GetByIdsAsync(ids);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        var persons = result.Value;
        return Ok(persons);
    }

    [HttpPost("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] PaginationFilterDTO pagination,
        [FromQuery] string searchTerm,
        [FromBody] IEnumerable<Guid>? ids = null)
    {
        var result = await _personService.SearchAsync(pagination, HttpContext, searchTerm, ids);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        var pagedData = result.Value;

        _paginationService.SetPaginationHeaders(Response, pagedData);

        return Ok(pagedData.Value);
    }
}
