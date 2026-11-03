using AutoMapper;
using Dapper;
using GOBID.Application.DTOs.Claim;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Claim;
using GOBID.Application.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;

namespace GOBID.Infrastructure.Services;

public class ClaimService : IClaimService
{
    private readonly IDbConnection _connection;
    private readonly IMapper _mapper;
    public ClaimService(IDbConnection connection, IMapper mapper)
    {
        _connection = connection;
        _mapper = mapper;
    }

    public async Task<Response<string>> UpsertAsync(ClaimUpsertDto[] dtos)
    {
        try
        {
            if (dtos == null || dtos.Length == 0)
                return new Response<string>("No se proporcionaron datos para insertar/actualizar", false);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            foreach (var dto in dtos)
            {
                var payload = JsonConvert.SerializeObject(dto, settings);
                var result = await _connection.QueryFirstOrDefaultAsync<string>(
                    "upsert_claim",
                    new { payload },
                    commandType: CommandType.StoredProcedure);

                if (string.IsNullOrEmpty(result))
                    return new Response<string>("No se pudo insertar/actualizar uno o más permisos", false);
            }

            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DeleteAsync(int claimId, Guid updatedBy)
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

            var payload = JsonConvert.SerializeObject(new { claim_id = claimId, updated_by = updatedBy }, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "delete_claim",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo eliminar el reclamo", false);

            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<List<ClaimVm>>> ListAsync(int? pageNumber, int? pageSize, ClaimFilterDto? filter)
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
                "read_claims",
                new { page_number = pageNumber, page_size = pageSize, filter = filterJson },
                commandType: CommandType.StoredProcedure);
            var wrapper = JsonConvert.DeserializeObject<ClaimResultWrapper>(result ?? "{}");
            var vms = _mapper.Map<List<ClaimVm>>(wrapper?.Data ?? []);
            return new Response<List<ClaimVm>>(vms, "OK");
        }
        catch (Exception ex)
        {
            return new Response<List<ClaimVm>>($"Error: {ex.Message}", false);
        }
    }
}
