using AutoMapper;
using Dapper;
using GOBID.Application.DTOs;
using GOBID.Application.DTOs.Pds;
using GOBID.Application.DTOs.Roles;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Roles;
using GOBID.Application.Wrappers;
using GOBID.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;

namespace GOBID.Infrastructure.Services;

public class RoleService: IRoleService
{
    private readonly IDbConnection _connection;
    private readonly IMapper _mapper;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RoleService> _logger;
    private readonly PrincipalContext _context;

    public RoleService(
        IDbConnection connection,
        IMapper mapper,
        IServiceProvider serviceProvider,
        ILogger<RoleService> logger,
        PrincipalContext context)
    {
        _connection = connection;
        _mapper = mapper;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _context = context;
    }

    public async Task<Response<string>> DeleteAsync(int roleId)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            var payload = JsonConvert.SerializeObject(new { role_id = roleId }, settings);
            var result = _connection.QueryFirstOrDefaultAsync<string>(
                "delete_role",
                new { payload },
                commandType: CommandType.StoredProcedure).Result;
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo eliminar el rol", false);
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    // Elimina todos los roles asociados a la posición (puede haber uno por cada
    // organización-aplicación). Cada rol se elimina con delete_role, igual que DeleteAsync.
    public async Task<Response<int>> DeleteByPositionAsync(Guid positionId)
    {
        try
        {
            var roleIds = await _context.Roles
                .AsNoTracking()
                .Where(r => r.PositionId == positionId)
                .Select(r => r.RoleId)
                .ToListAsync();

            var failedRoleIds = new List<int>();
            foreach (var roleId in roleIds)
            {
                var result = await DeleteAsync(roleId);
                if (!result.Succeeded)
                {
                    _logger.LogWarning(
                        "No se pudo eliminar el rol {RoleId} de la posición {PositionId}: {Message}",
                        roleId, positionId, result.Message);
                    failedRoleIds.Add(roleId);
                }
            }

            if (failedRoleIds.Count > 0)
                return new Response<int>(
                    $"No se pudieron eliminar los roles: {string.Join(", ", failedRoleIds)}", false);

            return new Response<int>(roleIds.Count, "Operación exitosa");
        }
        catch (Exception ex)
        {
            return new Response<int>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<RoleVm>> GetByIdAsync(int roleId)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            var pageNumber = 1;
            var pageSize = 1;
            var filter = JsonConvert.SerializeObject(
                new
                {
                    role_id = roleId
                },
                settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_roles",
                new { page_number = pageNumber, page_size = pageSize, filter },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result) || result == "{}")
                return new Response<RoleVm>("No se encontraron roles", false);

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<RoleDto>>(result);
            var role = wrapper?.Data?.FirstOrDefault();
            if (role == null)
                return new Response<RoleVm>("No se encontró el rol", false);
            var vm = _mapper.Map<RoleVm>(role);
            return new Response<RoleVm>(vm, "OK");
        }
        catch (Exception ex)
        {
            return new Response<RoleVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<List<RoleVm>>> ListAsync(int? pageNumber, int? pageSize, RoleFilterDto roleFilter)
    {
        if (pageNumber == null || pageNumber <= 0)
            pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100)
            pageSize = 50;
        try
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            var filter = JsonConvert.SerializeObject(roleFilter, settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_roles",
                new { page_number = pageNumber, page_size = pageSize, filter },
                commandType: CommandType.StoredProcedure);

            var json = JsonConvert.DeserializeObject<dynamic>(result ?? "{}");
            if (json?.data == null)
                return new Response<List<RoleVm>>([], "Sin resultados");

            var data = json.data.ToString();
            var roles = JsonConvert.DeserializeObject<List<RoleDto>>(data);
            var vms = _mapper.Map<List<RoleVm>>(roles);

            return new Response<List<RoleVm>>(vms, "OK");
        }
        catch (Exception ex)
        {
            return new Response<List<RoleVm>>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<RoleVm>> UpsertAsync(RoleUpsertDto dto)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            var payload = JsonConvert.SerializeObject(dto, settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "upsert_role",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<RoleVm>("No se pudo insertar/actualizar el rol", false);

            // Deserializar el resultado a RoleDto y mapear a RoleVm
            var roleDto = JsonConvert.DeserializeObject<RoleDto>(result);
            if (roleDto == null)
                return new Response<RoleVm>("No se pudo deserializar el rol", false);

            var vm = _mapper.Map<RoleVm>(roleDto);
            return new Response<RoleVm>(vm, "OK");
        }
        catch (Exception ex)
        {
            return new Response<RoleVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<PagedResponse<RoleVm>> ListByStructureAreaAsync(
        int organizationId,
        int organizationApplicationId,
        Guid structureAreaId,
        int? pageNumber,
        int? pageSize,
        string? search)
    {
        if (pageNumber == null || pageNumber <= 0)
            pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100)
            pageSize = 50;

        try
        {
            var allRoles = await FetchAllActiveRolesAsync(organizationApplicationId, search);
            var (scopedPositionIds, pdsAvailable) = await FetchScopedPositionIdsAsync(organizationId, structureAreaId);

            // 2. Split position-bearing vs positionless roles, and match the
            //    position-bearing ones against the structure scope, in one pass.
            var positionlessRoles = new List<RoleDto>();
            var matchedRoles = new List<RoleDto>();
            foreach (var role in allRoles)
            {
                if (role.PositionId == null)
                    positionlessRoles.Add(role);
                else if (pdsAvailable && scopedPositionIds.Contains(role.PositionId.Value))
                    matchedRoles.Add(role);
            }

            // 3. Combine matched position-roles + all positionless roles.
            var combined = matchedRoles
                .Concat(positionlessRoles)
                .OrderBy(r => r.RoleName)
                .ToList();

            // 4. Paginate in-memory (matching required the full candidate set first).
            var totalCount = combined.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize.Value);
            var pageItems = combined
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();

            var vms = _mapper.Map<List<RoleVm>>(pageItems);
            return new PagedResponse<RoleVm>(vms, pageNumber.Value, pageSize.Value, totalCount, totalPages, "OK");
        }
        catch (Exception ex)
        {
            return new PagedResponse<RoleVm>([], $"Error: {ex.Message}") { Succeeded = false };
        }
    }

    // Fetches ALL candidate active roles for the scope, paging through read_roles with a
    // page size the codebase already trusts (100). We loop until a short page (fewer than
    // fetchPageSize rows) is returned -- we deliberately do NOT rely on the wrapper's
    // total_pages, because read_roles emits snake_case keys while ResultWrapper expects
    // camelCase, so TotalPages deserializes to 0.
    private async Task<List<RoleDto>> FetchAllActiveRolesAsync(int organizationApplicationId, string? search)
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        const int fetchPageSize = 100;
        const int maxPages = 1000; // safety cap to guarantee termination
        var allRoles = new List<RoleDto>();
        var filter = JsonConvert.SerializeObject(
            new RoleFilterDto
            {
                OrganizationApplicationId = organizationApplicationId,
                IsActive = true,
                Search = search
            },
            settings);

        for (var currentPage = 1; currentPage <= maxPages; currentPage++)
        {
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_roles",
                new { page_number = currentPage, page_size = fetchPageSize, filter },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result) || result == "{}")
                break;

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<RoleDto>>(result);
            var pageRoles = wrapper?.Data;
            if (pageRoles == null || pageRoles.Count == 0)
                break;

            allRoles.AddRange(pageRoles);

            // A short page means we've reached the last page.
            if (pageRoles.Count < fetchPageSize)
                break;
        }

        return allRoles;
    }

    // Resolves every position that belongs to the requested structure area, including
    // positions under any of its descendant structures (e.g. selecting a Dirección Funcional
    // also brings in the positions of the Departamentos below it). Uses the bulk (unpaginated)
    // PDS endpoint -- pagination here would need a stable sort key to be safe across separate
    // round trips, which the structure-search endpoint doesn't guarantee for an empty search term.
    private async Task<(HashSet<Guid> PositionIds, bool PdsAvailable)> FetchScopedPositionIdsAsync(
        int organizationId, Guid structureAreaId)
    {
        // Resolve the PDS client lazily to avoid a DI construction cycle
        // (RoleService -> PdsApiService -> AuthService -> ... -> RoleService).
        // This mirrors how UserService resolves IPdsApiService.
        var pdsApiService = _serviceProvider.GetRequiredService<IPdsApiService>();

        var pdsResponse = await pdsApiService.GetPositionIdsByStructureAsync(organizationId, structureAreaId, includeDescendants: true);
        if (!pdsResponse.Succeeded)
        {
            // Graceful degradation: PDS unavailable -> caller falls back to
            // returning only the roles without a position (knowable from the DB).
            _logger.LogWarning(
                "PDS no disponible al resolver posiciones para estructura {StructureId} (org {OrgId}): {Message}. Se devuelven solo los roles sin posición.",
                structureAreaId, organizationId, pdsResponse.Message);
            return (new HashSet<Guid>(), false);
        }

        var scopedPositionIds = (pdsResponse.Data ?? []).ToHashSet();
        return (scopedPositionIds, true);
    }
}
