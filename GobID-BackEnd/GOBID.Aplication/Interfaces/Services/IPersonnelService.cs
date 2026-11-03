using GOBID.Application.DTOs.Personnel;
using GOBID.Application.ViewModels.Personnel;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IPersonnelService
{
    Task<Response<PersonnelVm>> GetAsync(Guid personnelId);
    Task<Response<PersonnelVm>> GetByPersonIdAsync(Guid personId);
    Task<PagedResponse<PersonnelVm>> ListAsync(int organizationId, int? pageNumber, int? pageSize, bool? isActive, Guid? positionId, int? roleId, string? searchTerm);
    Task<Response<PersonnelVm>> UpsertAsync(PersonnelUpsertDto dto);
    Task<Response<string>> DeleteAsync(Guid personnelId, Guid deletedBy);
    Task<Response<PersonnelSupervisorResultVm>> GetSupervisorPersonnelAsync(int organizationId, int roleId);
    Task<Response<List<PersonnelVm>>> GetSubordinatePersonnelAsync(int organizationId, int roleId);
    Task<Response<List<Guid>>> GetSubordinateUserIdsByPersonIdAsync(int organizationId, Guid personId);

    Task<Response<PersonnelSupervisorResultVm>> GetDirectManagerPersonnelAsync(int organizationId, int roleId);

    // Resuelve la identidad (UserId) + correo de los usuarios (tabla User) cuyo personal activo
    // ocupa alguna posición dentro de una estructura organizacional (departamento) de PDS.
    // Pensado para que sistemas consumidores (ej. SGSC) puedan notificar por departamento
    // ligando la notificación a la identidad real de GOBID, no solo al correo.
    Task<Response<List<UserContactDto>>> GetUserEmailsByOrganizationStructureAsync(int organizationId, Guid structureId);
}
