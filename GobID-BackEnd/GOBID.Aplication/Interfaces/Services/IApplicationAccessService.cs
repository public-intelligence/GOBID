using GOBID.Application.DTOs.ApplicationAccess;
using GOBID.Application.ViewModels.ApplicationAccess;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IApplicationAccessService
{
    public Task<Response<List<ApplicationAccessVm>>> GetAccessTypes(int pageNumber, int pageSize, ApplicationAccessFilterDto? filter);
    public Task<Response<string>> UpsertAsync(ApplicationAccessUpsertDto dto);
    public Task<Response<string>> DeleteAsync(int id);
}
