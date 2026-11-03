using GOBID.Application.DTOs.AccessType;
using GOBID.Application.ViewModels.AccessType;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IAccessTypeService
{
    public Task<Response<List<AccessTypeVm>>> ListAccessTypes(int pageNumber, int pageSize, AccessTypeFilterDto? filter);
    public Task<Response<string>> UpsertAsync(AccessTypeUpsertDto dto);
    public Task<Response<string>> DeleteAsync(int id);
    public Task<Response<AccessTypeVm>> GetAccessType(int pageNumber, int pageSize, int id);

}