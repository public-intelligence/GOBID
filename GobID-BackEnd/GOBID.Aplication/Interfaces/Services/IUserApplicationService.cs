using GOBID.Application.DTOs.UserApplication;
using GOBID.Application.ViewModels.UserApplication;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IUserApplicationService
{
    public Task<Response<string>> UpsertAsync(UserApplicationUpsertDto dto);
    public Task<Response<UserApplicationVm>> GetAsync(int pageNumber, int pageSize, Guid userApplicationId);
    public Task<Response<List<UserApplicationVm>>> ListAsync(int? pageNumber, int? pageSize, UserApplicationFilterDto? filter);
    public Task<Response<string>> DeleteAsync(Guid userApplicationId, Guid updatedBy);

}
