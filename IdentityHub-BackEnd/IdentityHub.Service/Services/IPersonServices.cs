using IdentityHub.Domain.DTOs.Organizations;
using IdentityHub.Domain.DTOs.Pagination;
using IdentityHub.Domain.DTOs.Persons;
using IdentityHub.Domain.Enums;
using IdentityHub.Domain.Response;
using Microsoft.AspNetCore.Http;

namespace IdentityHub.Service.Services;

public interface IPersonServices
{
    public Task<IHttpResult<IEnumerable<PersonDTO>>> GetAllAsync();
    public Task<IHttpResult<PagedResponseDTO<PersonDTO>>> GetAllAsync(PaginationFilterDTO paginationFilter, HttpContext httpContext, string? searchTerm = null, EnumStatus? status = null);
    public Task<IHttpResult<PersonDTO>> GetAsyncById(Guid id);
    public Task<IHttpResult<IEnumerable<PersonDTO>>> GetByIdsAsync(IEnumerable<Guid> ids);
    public Task<IHttpResult<PagedResponseDTO<PersonDTO>>> SearchAsync(PaginationFilterDTO paginationFilter, HttpContext httpContext, string searchTerm, IEnumerable<Guid>? ids = null);
    public Task<IHttpResult<PersonDTO>> CreateAsync(CreatePersonDTO createPersonDto);
    public Task<IHttpResult<PersonDTO>> UpdateAsync(
        Guid id,
        UpdatePersonDTO updatePersonDto
    );
    public Task<IHttpResult<PersonDTO>> GetAsyncByCedula(string cedula);
    public Task<IHttpResult<PersonDTO>> UpdateProfilePhotoAsync(Guid id, Guid profilePhotoId);
}
