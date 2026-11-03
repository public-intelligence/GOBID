using GOBID.Application.DTOs.UserRole;
using GOBID.Application.ViewModels.UserRole;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IUserRoleService
{
    public Task<Response<string>> UpsertAsync(UserRoleUpsertDto dto);
    public Task<Response<string>> DeleteAsync(int userRoleId, Guid updatedBy);
    public Task<Response<List<UserRoleVm>>> ListAsync(int? pageNumber, int? pageSize, int? userRoleId, Guid? userApp, int? roleId, bool? isActive);
    public Task<Response<UserRoleVm>> GetByIdAsync(int userRoleId);
}
