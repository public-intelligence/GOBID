using GOBID.Application.DTOs.EntityOrganization;
using GOBID.Application.Enums;
using GOBID.Application.Wrappers;


namespace GOBID.Application.Interfaces.Services;

public interface ILegalEntityService
{
    /// <summary>
    /// Crea una organizacion en la API externa
    /// </summary>
    /// <param name="entityOrganization">Datos de la persona a crear</param>
    /// <returns>Response con los datos de la persona creada</returns>
   public Task<Response<LegalEntityDTO>> CreateOrganizationAsync(CreateLegalEntityDTO CreateEntityOrganization);

    /// <summary>
    /// Obtiene una persona por su ID desde la API externa
    /// </summary>
    /// <param name="id">ID de la persona</param>
    /// <returns>Response con los datos de la persona</returns>
   public Task<Response<LegalEntityDTO>> GetOrganizationByIdAsync(Guid id);

    /// <summary>
    ///         
    /// </summary>
    /// <param name="identificationNumber">Número de identificación de la organización</param>
    /// <returns>Response con los datos de la organización</returns>
    public Task<Response<LegalEntityDTO>> GetOrganizationByIdentificationNumberAsync(string identificationNumber);

    /// <summary>
    /// Obtiene una lista de organizaciones en la API externa
    /// </summary>
    /// <returns>Response con los datos de la organización</returns>
    public Task<PagedResponse<LegalEntityDTO>> GetOrganizationsAsync(int? pageNumber, int? pageSize, string? searchTerm, EnumStatus? status);

    /// <summary>
    /// Actualiza una organización en la API externa
    /// </summary>
    /// <param name="updateOrganizationDto">Datos de la organización a actualizar</param>
    /// <returns>Response con los datos de la organización actualizada</returns>
   public Task<Response<LegalEntityDTO>> UpdateOrganizationAsync(Guid id, UpdateLegalEntityDTO updateOrganizationDto);
}
