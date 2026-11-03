using IdentityHub.Domain.DTOs.Organizations;
using IdentityHub.Domain.DTOs.Pagination;
using IdentityHub.Domain.DTOs.Persons;
using IdentityHub.Domain.Enums;
using IdentityHub.Domain.Response;
using Microsoft.AspNetCore.Http;

namespace IdentityHub.Service.Services;

public interface IOrganizationServices
{
    public Task<IHttpResult<LegalEntityDTO>> CreateAsync(CreateLegalEntityDTO organizationDto);
    public Task<IHttpResult<LegalEntityDTO>> GetByIdAsync(Guid id);
    public Task<IHttpResult<List<LegalEntityDTO>>> GetAllAsync();
    public Task<IHttpResult<PagedResponseDTO<LegalEntityDTO>>> GetAllAsync(PaginationFilterDTO paginationFilter, HttpContext httpContext, string? searchTerm = null, EnumStatus? status = null);
    public Task<IHttpResult<LegalEntityDTO>> UpdateAsync(Guid id, UpdateOrganizationDTO organizationDto);
    public Task<IHttpResult<LegalEntityDTO>> GetByIdentificationNumberAsync(string identificationNumber);
}
