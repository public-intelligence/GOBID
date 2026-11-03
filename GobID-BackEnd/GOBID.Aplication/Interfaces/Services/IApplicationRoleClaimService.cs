using GOBID.Application.DTOs.ApplicationRoleClaim;
using GOBID.Application.ViewModels.ApplicationRoleClaim;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IApplicationRoleClaimService
{
    /// <summary>
    /// Vincula uno o m\u00e1s claims a un rol de aplicaci\u00f3n M2M.
    /// Si el v\u00ednculo ya existe y est\u00e1 inactivo, lo reactiva.
    /// </summary>
    Task<Response<List<ApplicationRoleClaimVm>>> AddClaimsAsync(CreateApplicationRoleClaimDto dto, Guid createdBy);

    /// <summary>
    /// Lista los claims vinculados a un rol de aplicaci\u00f3n.
    /// </summary>
    Task<Response<List<ApplicationRoleClaimVm>>> ListByApplicationRoleAsync(int applicationRoleId);

    /// <summary>
    /// Elimina (desactiva) el v\u00ednculo entre un rol de aplicaci\u00f3n y un claim.
    /// </summary>
    Task<Response<string>> RemoveAsync(int applicationRoleClaimId, Guid updatedBy);
}
