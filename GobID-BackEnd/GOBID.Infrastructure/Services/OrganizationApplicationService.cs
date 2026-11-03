using AutoMapper;
using Dapper;
using GOBID.Application.DTOs;
using GOBID.Application.DTOs.Application;
using GOBID.Application.DTOs.Organization;
using GOBID.Application.DTOs.OrganizationApplication;
using GOBID.Application.DTOs.Roles;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Application;
using GOBID.Application.ViewModels.Organization;
using GOBID.Application.ViewModels.OrganizationApplication;
using GOBID.Application.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;

namespace GOBID.Infrastructure.Services;

public class OrganizationApplicationService : IOrganizationApplicationService
{
    private readonly IDbConnection _connection;
    private readonly IOrganizationService _organizationService;
    private readonly IApplicationService _applicationService;
    private readonly IMapper _mapper;
    private readonly IRoleService _roleService;
    public OrganizationApplicationService(IDbConnection connection, IOrganizationService organizationService, IApplicationService applicationService, IMapper mapper, IRoleService roleService)
    {
        _connection = connection;
        _organizationService = organizationService;
        _applicationService = applicationService;
        _mapper = mapper;
        _roleService = roleService;
    }

    public async Task<Response<string>> UpsertAsync(OrganizationApplicationUpsertDto dto)
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
                "upsert_org_app_map",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo insertar/actualizar la aplicación de la organización", false);
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al insertar/actualizar la aplicación de la organización: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DeleteAsync(int orgAppId)
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
            var payload = JsonConvert.SerializeObject(new { organization_application_id = orgAppId }, settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "delete_org_app_map",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo eliminar la aplicación de la organización", false);
            return new Response<string>("Operación exitosa", true);
        }
        catch
        {
            return new Response<string>("Error al eliminar la aplicación de la organización", false);
        }
    }
    public async Task<PagedResponse<OrganizationApplicationVm>> ListAsync(int? pageNumber, int? pageSize, OrganizationApplicationfilterDto? filter)
    {
        if (pageNumber == null || pageNumber <= 0)
            pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100)
            pageSize = 50;

        try
        {
            string? filterJson = null;
            if (filter != null)
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                };
                filterJson = JsonConvert.SerializeObject(filter, settings);
            }

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_org_app_maps",
                new { page_number = pageNumber, page_size = pageSize, filter = filterJson },
                commandType: CommandType.StoredProcedure);

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<OrganizationApplicationDto>>(result ?? "{}");

            var dtos = wrapper?.Data ?? new List<OrganizationApplicationDto>();
            var vms = _mapper.Map<List<OrganizationApplicationVm>>(dtos);

            var appResponse = await _applicationService.ListAsync(pageNumber, pageSize, new ApplicationFilterDto { ApplicationId = null });
            var applications = appResponse.Succeeded && appResponse.Data != null ? appResponse.Data : new List<ApplicationVm>();

            for (int i = 0; i < vms.Count; i++)
            {
                var dto = dtos[i];
                var vm = vms[i];

                vm.Application = applications.FirstOrDefault(a => a.ApplicationId == dto.ApplicationId);

                var roleFilter = new RoleFilterDto
                {

                    OrganizationApplicationId = dto.OrganizationApplicationId,
                    IsActive = true,
                    IsSystemRole = false

                };
                var rolesResponse = await _roleService.ListAsync(1, 100, roleFilter);
                var roles = rolesResponse.Data;

                vm.Role = roles?.ToArray();
                vm.RoleCount = roles?.Count ?? 0;
            }

            return new PagedResponse<OrganizationApplicationVm>(
                vms,
                wrapper?.CurrentPage ?? (pageNumber ?? 1),
                pageSize.Value,
                wrapper?.TotalTally ?? vms.Count,
                wrapper?.TotalPages ?? 1
            );
        }
        catch (Exception ex)
        {
            return new PagedResponse<OrganizationApplicationVm>(
                new List<OrganizationApplicationVm>(),
                "Error al listar las aplicaciones de la organización",
                new List<string> { ex.Message }
            );
        }
    }

    public async Task<Response<OrganizationApplicationVm>> GetAsync(int pageNumber, int pageSize, int orgAppId)
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

            // Pasar el filtro con el organization_application_id
            var filterObj = new { organization_application_id = orgAppId };
            var filterJson = JsonConvert.SerializeObject(filterObj, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_org_app_maps",
                new { page_number = pageNumber, page_size = pageSize, filter = filterJson },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result) || result == "{}")
                return new Response<OrganizationApplicationVm>("Sin resultados", false);

            var wrapper = JsonConvert.DeserializeObject<OrganizationapplicationResultWrapper>(result);
            if (wrapper?.Data == null || !wrapper.Data.Any())
                return new Response<OrganizationApplicationVm>("Sin datos", false);

            var orgApp = wrapper.Data.FirstOrDefault();
            if (orgApp == null)
                return new Response<OrganizationApplicationVm>("Sin datos", false);

            var orgFilter = new OrganizationFilterDto
            {
                Search = null
            };

            var orgsResponse = await _organizationService.GetOrganizationsAsync(pageNumber, pageSize, orgFilter, null);
            if (!orgsResponse.Succeeded || orgsResponse.Data == null)
                return new Response<OrganizationApplicationVm>("Error al obtener organizaciones", false);

            var organization = orgsResponse.Data.FirstOrDefault(o => o.OrganizationId == orgApp.OrganizationId);
            orgApp.Organization = organization;

            var appResponse = await _applicationService.GetAsync(1, 1, orgApp.ApplicationId);
            var applicationVm = appResponse.Succeeded ? appResponse.Data : null;

            var vm = new OrganizationApplicationVm
            {
                OrganizationApplicationId = orgApp.OrganizationApplicationId,
                OrganizationId = orgApp.OrganizationId,
                ApplicationId = orgApp.ApplicationId,
                IsActive = orgApp.IsActive,
                RowVersion = orgApp.RowVersion,
                Organization = organization,
                Application = applicationVm
            };

            return new Response<OrganizationApplicationVm>(vm, "OK");
        }
        catch (Exception ex)
        {
            return new Response<OrganizationApplicationVm>($"Error al obtener la aplicación de la organización: {ex.Message}", false);
        }
    }

    public async Task<PagedResponse<OrganizationApplicationVm>> GetByOrganizationIdAsync(int? pageNumber, int? pageSize, int organizationId, bool? excludeApp, string? search)
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

            var filterObj = new { organization_id = organizationId, exclude_app2 = excludeApp, search };
            var filter = JsonConvert.SerializeObject(filterObj, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_org_app_maps",
                new { page_number = pageNumber, page_size = pageSize, filter },
                commandType: CommandType.StoredProcedure);

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<OrganizationApplicationDto>>(result ?? "{}");

            var dtos = wrapper?.Data ?? new List<OrganizationApplicationDto>();
            var vms = _mapper.Map<List<OrganizationApplicationVm>>(dtos);

            // Enriquecer con aplicaciones y roles (igual que ListAsync)
            var appResponse = await _applicationService.ListAsync(1, 50, new ApplicationFilterDto { ApplicationId = null });
            var applications = appResponse.Succeeded && appResponse.Data != null ? appResponse.Data : new List<ApplicationVm>();

            for (int i = 0; i < vms.Count; i++)
            {
                var dto = dtos[i];
                var vm = vms[i];

                vm.Application = applications.FirstOrDefault(a => a.ApplicationId == dto.ApplicationId);

                var roleFilter = new RoleFilterDto
                {
                    OrganizationApplicationId = dto.OrganizationApplicationId,
                    IsActive = true,
                    Search = search,
                    IsSystemRole = false
                };
                var rolesResponse = await _roleService.ListAsync(1, 50, roleFilter);
                var roles = rolesResponse.Data;

                vm.Role = roles?.ToArray();
                vm.RoleCount = roles?.Count ?? 0;
            }

            return new PagedResponse<OrganizationApplicationVm>(
                vms,
                wrapper?.CurrentPage ?? (pageNumber ?? 1),
                pageSize.Value,
                wrapper?.TotalTally ?? vms.Count,
                wrapper?.TotalPages ?? 1
            );
        }
        catch (Exception ex)
        {
            return new PagedResponse<OrganizationApplicationVm>(
                new List<OrganizationApplicationVm>(),
                "Error al listar las aplicaciones de la organización",
                new List<string> { ex.Message }
            );
        }
    }

}
