using GOBID.Application.DTOs.Policies;
using GOBID.Application.ViewModels.Policies;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IPolicyService
{
    Task<Response<List<PolicyVm>>> ListByOrganizationAsync(int organizationId, bool? isActive);
    Task<Response<PolicyVm>> GetByIdAsync(Guid policyId);
    Task<Response<string>> UpsertAsync(PolicyUpsertDto dto);
    Task<Response<string>> DeleteAsync(Guid policyId);
}
