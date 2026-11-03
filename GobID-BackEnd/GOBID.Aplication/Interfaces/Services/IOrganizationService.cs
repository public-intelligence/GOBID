using GOBID.Application.DTOs.Organization;
using GOBID.Application.ViewModels.Organization;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IOrganizationService
{
    public Task<PagedResponse<OrganizationVm>> GetOrganizationsAsync(int? pageNumber, int? pageSize, OrganizationFilterDto? filter, int? organizationId);
    public Task<Response<OrganizationVm>> UpsertAsync(OrganizationUpsertDto dto);
    public Task<Response<string>> DeleteAsync(int orgId, Guid deletedBy);
    public Task<Response<OrganizationVm>> GetAsync(int pageNumber, int pageSize, int orgId);
}