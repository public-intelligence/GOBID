using GOBID.Application.DTOs.Personnel;
using GOBID.Application.Enums;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Http;

namespace GOBID.Application.Interfaces.Services;

/// <summary>
/// Servicio para interactuar con la API externa de personas
/// </summary>
public interface IPersonApiService
{
    /// <summary>
    /// Crea una persona en la API externa
    /// </summary>
    /// <param name="createPersonDto">Datos de la persona a crear</param>
    /// <returns>Response con los datos de la persona creada</returns>
    public Task<Response<IdentityPersonResponseDTO>> CreatePersonAsync(CreatePersonDTO createPersonDto);

    /// <summary>
    /// Obtiene una persona por su ID desde la API externa
    /// </summary>
    /// <param name="id">ID de la persona</param>
    /// <returns>Response con los datos de la persona</returns>
    public Task<Response<IdentityPersonResponseDTO>> GetPersonByIdAsync(Guid id);

    /// <summary>
    /// Obtiene múltiples personas por sus IDs en una sola llamada HTTP (POST /person/bulk).
    /// Usar en lugar de múltiples llamadas a GetPersonByIdAsync cuando se trabaja con listas.
    /// </summary>
    public Task<Response<List<IdentityPersonResponseDTO>>> GetPersonsByIdsAsync(IEnumerable<Guid> ids);

    /// <summary>
    /// Obtiene una lista de personas en la API externa
    /// </summary>
    /// <returns>Response con los datos de la persona</returns>
    public Task<PagedResponse<IdentityPersonResponseDTO>> GetPersonsAsync(int? pageNumber, int? pageSize, string? searchTerm, EnumStatus? status);

    /// <summary>
    /// Busca personas por nombre, apellido o número de identificación (POST /person/search?searchTerm=...).
    /// Acepta una lista opcional de IDs (en el body) para acotar la búsqueda dentro de un subconjunto de personas.
    /// </summary>
    public Task<PagedResponse<IdentityPersonResponseDTO>> SearchPersonsAsync(int? pageNumber, int? pageSize, string searchTerm, IEnumerable<Guid>? ids = null);

    /// <summary>
    /// Actualiza una persona en la API externa
    /// </summary>
    /// <param name="updatePersonDto">Datos de la persona a crear</param>
    /// <returns>Response con los datos de la persona creada</returns>
    public Task<Response<IdentityPersonResponseDTO>> UpdatePersonAsync(Guid id, UpdatePersonDTO updatePersonDto);

    /// <summary>
    /// Obtiene una persona por su cédula desde la API externa
    /// </summary>
    /// <param name="cedula">Cédula de la persona</param>
    /// <returns>Response con los datos de la persona</returns>
    public Task<Response<IdentityPersonResponseDTO>> GetPersonByCedulaAsync(string cedula, bool isPassport);

    /// <summary>
    /// Actualiza el ProfilePhotoId de una persona en la API externa (IdentityHub)
    /// </summary>
    /// <param name="id">ID de la persona</param>
    /// <param name="profilePhotoId">ID del documento (foto) devuelto por el DMS</param>
    public Task<Response<IdentityPersonResponseDTO>> UpdateProfilePhotoAsync(Guid id, Guid profilePhotoId);

    /// <summary>
    /// Sube la foto de perfil de una persona al DMS y guarda el documentId resultante
    /// como ProfilePhotoId en IdentityHub.
    /// </summary>
    /// <param name="personId">ID de la persona</param>
    /// <param name="file">Archivo de la foto</param>
    public Task<Response<IdentityPersonResponseDTO>> UploadPersonPhotoAsync(Guid personId, IFormFile file);

    /// <summary>
    /// Obtiene el contenido binario de la foto de perfil de una persona desde el DMS.
    /// Busca el ProfilePhotoId de la persona en IdentityHub y luego descarga el documento del DMS.
    /// </summary>
    /// <param name="personId">ID de la persona</param>
    public Task<Response<PersonPhotoDTO>> GetPersonPhotoAsync(Guid personId);
}
