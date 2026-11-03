using AutoMapper;
using Dapper;
using GOBID.Application.DTOs.AccessType;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.AccessType;
using GOBID.Application.ViewModels.Organization;
using GOBID.Application.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;

namespace GOBID.Infrastructure.Services;

public class AccessTypeService : IAccessTypeService
{
    private readonly IDbConnection _connection;
    private readonly IMapper _mapper;
    public AccessTypeService(IDbConnection connection, IMapper mapper)
    {
        _connection = connection;
        _mapper = mapper;
    }

    public async Task<Response<string>> DeleteAsync(int id)
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
            var payload = JsonConvert.SerializeObject(new { access_type_id = id }, settings);

            var result = await _connection.ExecuteAsync(
                "delete_access_type",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (result == 0)
                return new Response<string>("No se encontró el registro solicitado", false);
            return new Response<string>("Registro eliminado correctamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<AccessTypeVm>> GetAccessType(int pageNumber, int pageSize, int id)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_access_types",
                new { page_number = pageNumber, page_size = pageSize, filter = (object?)null },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result))
                return new Response<AccessTypeVm>("Sin resultados", false);

            var wrapper = JsonConvert.DeserializeObject<AccessTypeResultWrapper>(result);
            var accesses = wrapper?.Data ?? new List<AccessTypeDto>();
            var access = accesses.FirstOrDefault(a => a.AccessTypeId == id);

            if (access == null)
                return new Response<AccessTypeVm>("No se encontró el registro solicitado", false);

            var vms = _mapper.Map<AccessTypeVm>(access);
            return new Response<AccessTypeVm>(vms, "OK");
        }
        catch (Exception ex)
        {
            return new Response<AccessTypeVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<List<AccessTypeVm>>> ListAccessTypes(int pageNumber, int pageSize, AccessTypeFilterDto? filter)
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

            string? filterJson = null;
            if (filter != null)
                filterJson = JsonConvert.SerializeObject(filter, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_access_types",
                new { page_number = pageNumber, page_size = pageSize, filter = filterJson },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result) || result == "{}")
                return new Response<List<AccessTypeVm>>([], "Sin resultados");

            var wrapper = JsonConvert.DeserializeObject<AccessTypeResultWrapper>(result);
            var vms = _mapper.Map<List<AccessTypeVm>>(wrapper?.Data ?? []);
            return new Response<List<AccessTypeVm>>(vms, "OK");
        }
        catch (Exception ex)
        {
            return new Response<List<AccessTypeVm>>($"Error: {ex.Message}", false);
        }

    }

    public async Task<Response<string>> UpsertAsync(AccessTypeUpsertDto dto)
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
                "upsert_access_type",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo insertar/actualizar la organización", false);

            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }
}
