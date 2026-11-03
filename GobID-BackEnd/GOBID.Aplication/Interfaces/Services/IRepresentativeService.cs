using GOBID.Aplication.ViewModels.Representative;
using GOBID.Application.Wrappers;

namespace GOBID.Aplication.Interfaces.Services
{
    public interface IRepresentativeService
    {
        //Task<Response<RepresentativeVm>> InsertAsync(RepresentativeDto dto);

        //Task<Response<RepresentativeVm>> UpdateAsync(int id, RepresentativeDto dto);

        Task<Response<RepresentativeVm>> GetByIdAsync(int id);

        Task<PagedResponse<IList<RepresentativeVm>>> GetPagedListAsync(int pageNumber, int pageSize, string? filter = null);

 

    }
}
