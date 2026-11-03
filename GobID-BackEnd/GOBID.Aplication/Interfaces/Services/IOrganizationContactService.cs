using GOBID.Application.DTOs.OrganizationContact;
using GOBID.Application.ViewModels.OrganizationContact;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IOrganizationContactService
{
    public Task<Response<string>> UpsertAsync(OrganizationContactUpsertDto[] dto);
    public Task<Response<string>> DeleteAsync(int ContactId);
    public Task<Response<List<OrganizationContactVm>>> ListAsync(int organizationId);
}
