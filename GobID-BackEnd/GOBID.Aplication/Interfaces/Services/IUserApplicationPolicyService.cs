using GOBID.Application.DTOs.UserApplicationPolicy;
using GOBID.Application.ViewModels.UserApplicationPolicy;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IUserApplicationPolicyService
{
    Task<Response<List<UserApplicationPolicyVm>>> ListByUserApplicationAsync(Guid userApplicationId, bool? isActive);
    Task<Response<string>> AssignAsync(UserApplicationPolicyUpsertDto dto);
    Task<Response<string>> RemoveAsync(Guid userApplicationPolicyId, Guid updatedBy);
}
