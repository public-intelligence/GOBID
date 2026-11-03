using GOBID.Application.DTOs.Claim;
using GOBID.Application.ViewModels.Claim;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IClaimService
{
    public Task<Response<string>> UpsertAsync(ClaimUpsertDto[] dto);
    public Task<Response<string>> DeleteAsync(int claimId, Guid updatedBy);
    public Task<Response<List<ClaimVm>>> ListAsync(int? pagenumber, int? pageSize, ClaimFilterDto? dto);
}
