using GOBID.Application.DTOs.Application;
using GOBID.Application.ViewModels.Application;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IApplicationService
{
    public Task<Response<string>> UpsertAsync(ApplicationUpsertDto dto);
    public Task<Response<string>> DeleteAsync(Guid appId, ApplicationDeleteDto dto);
    public Task<PagedResponse<ApplicationVm>> ListAsync(int? pageNumber, int? pageSize, ApplicationFilterDto? filter);
    public Task<Response<ApplicationVm>> GetAsync(int? pageNumber, int? pageSize, Guid appId);
    public Task<Response<string>> GetGeneratestring(Guid appId);
    public Task<Response<string>> DuplicateAsync(Guid appId, Guid userId);
}
