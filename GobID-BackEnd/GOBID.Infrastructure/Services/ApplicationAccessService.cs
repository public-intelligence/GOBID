using AutoMapper;
using Dapper;
using GOBID.Application.DTOs.ApplicationAccess;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.ApplicationAccess;
using GOBID.Application.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;

namespace GOBID.Infrastructure.Services;

public class ApplicationAccessService : IApplicationAccessService
{
    private readonly IDbConnection _connection;
    private readonly IMapper _mapper;

    public ApplicationAccessService(IDbConnection connection, IMapper mapper)
    {
        _connection = connection;
        _mapper = mapper;
    }

    public async Task<Response<List<ApplicationAccessVm>>> GetAccessTypes(int pageNumber, int pageSize, ApplicationAccessFilterDto? filter)
    {
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
                "read_app_accesses",
                new { page_number = pageNumber, page_size = pageSize, filter = filterJson },
                commandType: CommandType.StoredProcedure);
            var wrapper = JsonConvert.DeserializeObject<ApplicationAccessResultWrapper>(result ?? "{}");
            var vms = _mapper.Map<List<ApplicationAccessVm>>(wrapper?.Data ?? []);
            return new Response<List<ApplicationAccessVm>>(vms, "OK");
        }
        catch (Exception ex)
        {
            return new Response<List<ApplicationAccessVm>>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> UpsertAsync(ApplicationAccessUpsertDto dto)
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
                "upsert_app_access",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo insertar/actualizar el acceso", false);
            var access = JsonConvert.DeserializeObject<ApplicationAccessDto>(result);
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DeleteAsync(int id)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "delete_app_access",
                new { application_access_id = id },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo eliminar el acceso", false);
            return new Response<string>("Operación exitosa");
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }
}
