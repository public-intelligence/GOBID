using GOBID.Application.DTOs.Policies;
using GOBID.Application.ViewModels.Policies;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IPolicyTypeService
{
    Task<Response<List<PolicyTypeVm>>> ListAsync(bool? isActive);
    Task<Response<PolicyTypeVm>> GetByIdAsync(int policyTypeId);
    Task<Response<string>> UpsertAsync(PolicyTypeUpsertDto dto);
    Task<Response<string>> DeleteAsync(int policyTypeId);
}
