namespace IdentityHub.Domain.DTOs.Pagination;

public record PaginationFilterDTO
{
    
    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public PaginationFilterDTO()
    {
        PageNumber = 1;
        PageSize = 20;

    }
    public PaginationFilterDTO(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize > 100 ? 100 : pageSize;
    }
}
