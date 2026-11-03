using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Personnel;
using GOBID.Application.Enums;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/identityHub/[controller]")]
[ApiController]
public class PersonController : OwnBaseController
{
    private readonly IPersonApiService _personApiService;
    private readonly IPersonnelService _personnelService;

    public PersonController(
        IPersonApiService personApiService,
        IPersonnelService personnelService,
        IAuthService authService) : base(authService)
    {
        _personApiService = personApiService;
        _personnelService = personnelService;
    }

    /// <summary>
    /// Crea una persona en la API externa
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<Response<IdentityPersonResponseDTO>>> CreateAsync([FromBody] CreatePersonDTO createPersonDto)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("edit_person"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _personApiService.CreatePersonAsync(createPersonDto);

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
    public async Task<ActionResult<Response<IdentityPersonResponseDTO>>> GetAsyncById(Guid id)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("list_person"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _personApiService.GetPersonByIdAsync(id);

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
    public async Task<ActionResult<Response<IdentityPersonResponseDTO>>> GetAsync([FromQuery] int? pageNumber, [FromQuery] int? pageSize, [FromQuery] string? search, [FromQuery] EnumStatus? status)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("list_person"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _personApiService.GetPersonsAsync(pageNumber, pageSize, search, status);
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
    public async Task<ActionResult<Response<IdentityPersonResponseDTO>>> UpdateAsync(Guid id, UpdatePersonDTO updatePerson)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("edit_person"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var response = await _personApiService.UpdatePersonAsync(id, updatePerson);

        if (!response.Succeeded)
            return NotFound(response);

        return Ok(response);
    }

    /// <summary>
    /// Endpoint M2M: sube la foto de perfil de una persona al DMS y guarda el documentId
    /// resultante como ProfilePhotoId en IdentityHub. 
    /// Es PUT (no POST) porque siempre reemplaza/asigna la foto del recurso {id}/photo,
    /// sin importar si la persona ya tenía una foto asignada.
    /// </summary>
    [HttpPut("{id}/photo")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<Response<IdentityPersonResponseDTO>>> UploadProfilePhotoAsync(Guid id, IFormFile file)
    {
        
        if (file == null || file.Length == 0)
            return BadRequest(new Response<object>(null!, "Debe proporcionar un archivo de foto."));

        var response = await _personApiService.UploadPersonPhotoAsync(id, file);
        if (!response.Succeeded)
            return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    /// Descarga la foto de perfil de una persona desde el DMS, usando el ProfilePhotoId
    /// almacenado en IdentityHub. Puede ser consumido por el front (usuario normal con
    /// permiso 'list_personnel') o por aplicaciones M2M (con 'access:M2M' + 'list_personnel').
    /// </summary>
    [HttpGet("{id}/photo")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProfilePhotoAsync(Guid id)
    {

        var response = await _personApiService.GetPersonPhotoAsync(id);
        if (!response.Succeeded || response.Data == null)
            return NotFound(new Response<object>(null!, response.Message ?? "No se pudo obtener la foto de perfil."));

        var photo = response.Data;
        // Sin fileName: no se fija Content-Disposition: attachment, así el binario
        // se puede mostrar inline (<img>) o reenviarse tal cual desde otro backend.
        return File(photo.Content, photo.ContentType);
    }

    /// <summary>
    /// Obtiene una persona por su cédula desde la API externa,
    /// enriquecida con su cargo e información de posición si está registrada como personal.
    /// </summary>
    /// <param name="cedula">Cédula de la persona</param>
    /// <returns>Response con los datos de la persona y su posición (si aplica)</returns>
    [HttpGet("cedula/{cedula}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Response<PersonWithPositionResponseDto>>> GetAsyncByCedula(string cedula)
    {
        var permissions = await GetPermissionFromTokenAsync();
        if (permissions == null || !permissions.Any())
            return Unauthorized("No se pudo obtener el permiso desde el token.");

        if (!permissions.Contains("list_person"))
            return StatusCode(403, new Response<object>(null, "No tienes permiso para realizar esta acción."));

        var personResponse = await _personApiService.GetPersonByCedulaAsync(cedula, false);

        if (!personResponse.Succeeded)
            return NotFound(new Response<PersonWithPositionResponseDto>(personResponse.Message, false));

        var person = personResponse.Data!;

        var result = new PersonWithPositionResponseDto
        {
            Id = person.Id,
            FirstName = person.FirstName,
            LastName = person.LastName,
            IdentificationNumber = person.IdentificationNumber,
            Gender = person.Gender,
            Birthdate = person.Birthdate,
            Email = person.Email,
            PhoneNumber = person.PhoneNumber,
            IsExternal = person.IsExternal,
            Status = person.Status
        };

        var personnelResponse = await _personnelService.GetByPersonIdAsync(person.Id);
        if (personnelResponse.Succeeded && personnelResponse.Data != null)
        {
            var personnel = personnelResponse.Data;
            result.PersonnelId = personnel.PersonnelId;
            result.PositionId = personnel.PositionId ?? personnel.PositionNominateId;
            result.OrganizationId = personnel.OrganizationId;
            result.IsActivePersonnel = personnel.IsActive;
            result.Position = personnel.Position;
        }

        return Ok(new Response<PersonWithPositionResponseDto>(result, "Ok"));
    }

    /// <summary>
    /// Obtiene una persona por su cédula desde la API externa
    /// </summary>
    /// <param name="cedula">Cédula de la persona</param>
    /// <returns>Response con los datos de la persona</returns>
    [AllowAnonymous]
    [HttpGet("/api/v{version:apiVersion}/public/identityHub/Person/cedula/{cedula}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Response<IdentityPersonResponseDTO>>> GetAsyncByCedulaPublic(string cedula, bool isPassport)
    {
        var response = await _personApiService.GetPersonByCedulaAsync(cedula, isPassport);

        if (!response.Succeeded)
            return NotFound(response);

        return Ok(response);
    }
}