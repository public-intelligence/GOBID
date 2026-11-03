using GOBID.Application.DTOs.Roleclaim;
using GOBID.Application.ViewModels.RoleClaim;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IRoleClaimService
{
    public Task<Response<List<RoleClaimVm>>> ListAsync(int? pageNumber, int? pageSize, int? roleId, bool? isActive);
    public Task<Response<string>> UpsertAsync(RoleClaimUpsertDto[] dto);
    public Task<Response<string>> DeleteAsync(int roleClaimId, Guid deletedBy);
    public Task<Response<RoleClaimVm>> GetAsync(int roleClaimId);
}
