namespace GOBID.Application.Wrappers
{
    public class PagedResponse<T> : Response<List<T>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public PagedResponse(List<T> data, int pageNumber, int pageSize, int totalCount,int totalPages, string? message = null)
            : base(data, message)
        {
            this.PageNumber = pageNumber;
            this.PageSize = pageSize;
            this.TotalCount = totalCount;
            this.TotalPages = totalPages;
        }

        public PagedResponse(List<T> data, string message, List<string>? errors = null)
            : base(data, message)
        {
            this.PageNumber = 0;
            this.PageSize = 0;
            this.TotalCount = 0;
            this.TotalPages = 0;
        }

        public PagedResponse(string message, bool succeeded)
            : base(message, succeeded)
        {
            this.PageNumber = 0;
            this.PageSize = 0;
            this.TotalCount = 0;
            this.TotalPages = 0;
        }
    }
}
