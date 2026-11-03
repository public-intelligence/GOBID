using GOBID.Application.DTOs.ApplicationRole;
using GOBID.Application.ViewModels.ApplicationRole;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IApplicationRoleService
{
    Task<Response<ApplicationRoleVm>> GetByIdAsync(int applicationRoleId);

    Task<Response<List<ApplicationRoleVm>>> ListAsync(
        int? pageNumber,
        int? pageSize,
        Guid? appCredentialId,
        int? organizationApplicationId,
        int? roleId,
        bool? isActive);

    Task<Response<ApplicationRoleVm>> UpsertAsync(ApplicationRoleUpsertDto dto, Guid updatedBy);

    Task<Response<string>> DeleteAsync(int applicationRoleId, Guid updatedBy);
}
