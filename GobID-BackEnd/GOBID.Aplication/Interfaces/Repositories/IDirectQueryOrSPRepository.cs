using GOBID.Aplication.DTOs;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Repositories
{
    public interface IDirectQueryOrSPRepository
    {
        Task<Response<SpResult>> SpTestResult(string nombre, string apellido); 
       
    }
}
