using AutoMapper;
using Dapper;
using GOBID.Application.DTOs.UserApplication;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Data;
using GOBID.Application.ViewModels.UserApplication;
using GOBID.Application.DTOs;
using Microsoft.Data.SqlClient;

namespace GOBID.Infrastructure.Services;

public class UserApplicationService: IUserApplicationService
{
    private readonly IDbConnection _connection;
    private readonly IMapper _mapper;
    private readonly IOrganizationApplicationService _organizationApplicationService;
    private readonly IUserRoleService _userRoleService;

    public UserApplicationService(IDbConnection connection, IMapper mapper, IOrganizationApplicationService organizationApplicationService, IUserRoleService userRoleService)
    {
        _connection = connection;
        _mapper = mapper;
        _organizationApplicationService = organizationApplicationService;
        _userRoleService = userRoleService;
    }

    public async Task<Response<string>> UpsertAsync(UserApplicationUpsertDto dto)
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
                "upsert_user_application",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo insertar/actualizar la solicitud de usuario", false);

            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DeleteAsync(Guid userApplicationId, Guid updatedBy)
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

            // El SP espera: user_application_id, updated_by, expected_row_version (Base64)
            var payloadObj = new
            {
                user_application_id = userApplicationId,
                updated_by = updatedBy
            };
            var payload = JsonConvert.SerializeObject(payloadObj, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "delete_user_application",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo eliminar la solicitud de usuario", false);

            return new Response<string>("Operación exitosa", true);
        }
        catch (SqlException ex) when (ex.Number == 51000)
        {
            try
            {
                var err = JsonConvert.DeserializeObject<dynamic>(ex.Message);
                var msg = (string)(err?.error ?? ex.Message);
                return new Response<string>($"Error: {msg}", false);
            }
            catch
            {
                return new Response<string>($"Error SQL: {ex.Message}", false);
            }
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<UserApplicationVm>> GetAsync(int pageNumber, int pageSize, Guid userAppId)
    {
        try
        {
            var filter = new { user_application_id = userAppId };
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            var filterJson = JsonConvert.SerializeObject(new { user_application_id = userAppId }, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_user_applications",
                new { page_number = pageNumber, page_size = pageSize, filter = filterJson },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result))
                return new Response<UserApplicationVm>("No se encontró la solicitud de usuario", false);

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<UserApplicationDto>>(result);
            var dto = wrapper?.Data?.FirstOrDefault();
            if (dto == null)
                return new Response<UserApplicationVm>("No se encontró la solicitud de usuario", false);

            var vm = _mapper.Map<UserApplicationVm>(dto);

            
            var orgAppResponse = await _organizationApplicationService.GetAsync(1, 1, vm.OrganizationApplicationId);
            if (orgAppResponse.Succeeded && orgAppResponse.Data != null)
                  vm.OrganizationApplication = orgAppResponse.Data;
            else
                vm.OrganizationApplication = null;

            var userRoleResponse = await _userRoleService.ListAsync(pageNumber, pageSize, null, vm.UserApplicationId, null, null);
            if (userRoleResponse.Succeeded && userRoleResponse.Data != null)
                vm.UserRole = userRoleResponse.Data.FirstOrDefault();
            else
                vm.UserRole = null;

            return new Response<UserApplicationVm>(vm, "Ok");
        }
        catch (Exception ex)
        {
            return new Response<UserApplicationVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<List<UserApplicationVm>>> ListAsync(int? pageNumber, int? pageSize, UserApplicationFilterDto? filter)
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
                "read_user_applications",
                new { page_number = pageNumber, page_size = pageSize, filter = filterJson },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result))
                return new Response<List<UserApplicationVm>>([], "Sin resultados");

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<UserApplicationDto>>(result);

            var vms = _mapper.Map<List<UserApplicationVm>>(wrapper?.Data ?? []);

            foreach (var vm in vms)
            {
                var orgAppResponse = await _organizationApplicationService.GetAsync(1, 1, vm.OrganizationApplicationId);
                if (orgAppResponse.Succeeded && orgAppResponse.Data != null)
                    vm.OrganizationApplication = orgAppResponse.Data;
                else
                    vm.OrganizationApplication = null;
            }

            return new Response<List<UserApplicationVm>>(vms, "OK");
        }
        catch
        {
            return new Response<List<UserApplicationVm>>("Error al listar las aplicaciones", false);
        }
    }
}
