using GOBID.Application.DTOs.Pds;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IPdsApiService
{
    Task<Response<PdsPositionDto>> GetPositionAsync(int organizationId, Guid positionId);
    Task<PagedResponse<PdsPositionDto>> SearchPositionsAsync(int organizationId, string? q, int? pageNumber, int? pageSize);
    Task<PagedResponse<PdsPositionDto>> SearchOrganizationStructurePositionsAsync(int organizationId, Guid structureId, string? q, int? pageNumber, int? pageSize, bool includeDescendants = false);
    Task<PagedResponse<PdsPositionDto>> GetPositionsAsync(int organizationId, int? pageNumber, int? pageSize);
    Task<Response<List<PdsPositionSupervisorLevelDto>>> GetPositionSupervisorChainAsync(int organizationId, IEnumerable<Guid> positionIds);
    Task<Response<List<PdsPositionSupervisorLevelDto>>> GetSubordinatePositionsAsync(int organizationId, Guid supervisorPositionId);
    Task<Response<List<PdsPositionDto>>> GetPositionsByIdsAsync(int organizationId, IEnumerable<Guid> positionIds);
    Task<Response<List<Guid>>> GetPositionIdsByStructureAsync(int organizationId, Guid structureId, bool includeDescendants = false);
    Task<Response<PdsPositionDto?>> GetDirectManagerByPositionAsync(int organizationId, Guid positionId);
    Task<PagedResponse<PdsLocalidadDto>> GetLocalidadesAsync(int organizationId, PdsLocalidadFilterDto? filter = null, int? pageNumber = null, int? pageSize = null);
    Task<Response<List<PdsLocalidadDto>>> GetLocalidadesByIdsAsync(IEnumerable<Guid> localidadIds);
}
