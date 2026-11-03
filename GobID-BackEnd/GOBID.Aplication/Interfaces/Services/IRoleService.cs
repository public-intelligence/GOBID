using GOBID.Application.DTOs.Roles;
using GOBID.Application.ViewModels.Roles;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IRoleService
{
    public Task<Response<RoleVm>> UpsertAsync(RoleUpsertDto dto);
    public Task<Response<string>> DeleteAsync(int roleId);
    public Task<Response<int>> DeleteByPositionAsync(Guid positionId);
    public Task<Response<List<RoleVm>>> ListAsync(int? pageNumber, int? pageSize, RoleFilterDto roleFilter);
    public Task<Response<RoleVm>> GetByIdAsync(int roleId);
    public Task<PagedResponse<RoleVm>> ListByStructureAreaAsync(
        int organizationId,
        int organizationApplicationId,
        Guid structureAreaId,
        int? pageNumber,
        int? pageSize,
        string? search);
}