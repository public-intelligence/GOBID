using GOBID.Application.DTOs.OrganizationApplication;
using GOBID.Application.ViewModels.OrganizationApplication;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IOrganizationApplicationService
{
    public Task<Response<string>> UpsertAsync(OrganizationApplicationUpsertDto dto);

    public Task<Response<string>> DeleteAsync(int orgApp);

    public Task<PagedResponse<OrganizationApplicationVm>> ListAsync(int? pageNumber, int? pageSize, OrganizationApplicationfilterDto? filter);
    public Task<Response<OrganizationApplicationVm>> GetAsync(int pageNumber, int pageSize, int orgAppId );

    public Task<PagedResponse<OrganizationApplicationVm>> GetByOrganizationIdAsync(int? pageNumber, int? pageSize, int organizationId, bool? exludeApp, string? search);

}