using IdentityHub.Domain.DTOs.Pagination;
using Microsoft.AspNetCore.Http;


namespace IdentityHub.Service.Services;

public interface IPaginationService
{
    PagedResponseDTO<T> BuildPaginationUrl<T>(HttpContext httpContext, int pageNumber, int pageSize, int totalCount, IEnumerable<T> entityDtos);
    void SetPaginationHeaders<T>(HttpResponse httpResponse, PagedResponseDTO<T> pagedResponseDto);
}

