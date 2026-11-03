using AutoMapper;
using Dapper;
using GOBID.Application.DTOs;
using GOBID.Application.DTOs.Roleclaim;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.RoleClaim;
using GOBID.Application.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;

namespace GOBID.Infrastructure.Services;

public class RoleClaimService : IRoleClaimService
{
    private readonly IDbConnection _connection;
    private readonly IMapper _mapper;
    private readonly JsonSerializerSettings _jsonSettings;

    public RoleClaimService(IDbConnection connection, IMapper mapper)
    {
        _connection = connection;
        _mapper = mapper;
        _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };
    }

    public async Task<Response<string>> UpsertAsync(RoleClaimUpsertDto[] dtos)
    {
        try
        {
            if (dtos == null || dtos.Length == 0)
                return new Response<string>("No se proporcionaron datos para insertar/actualizar", false);

            foreach (var dto in dtos)
            {
                var payload = JsonConvert.SerializeObject(new
                {
                    role_id = dto.RoleId,
                    claim_id = dto.ClaimId,
                    is_active = dto.IsActive ? 1 : 0,
                    created_by = dto.CreatedBy,
                    updated_by = dto.UpdatedBy
                }, _jsonSettings);
                var result = await _connection.QueryFirstOrDefaultAsync<string>(
                    "upsert_role_claim",
                    new { payload },
                    commandType: CommandType.StoredProcedure);
                if (string.IsNullOrEmpty(result))
                    return new Response<string>("No se pudo insertar/actualizar el rol de reclamación", false);
            }
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DeleteAsync(int roleClaimId, Guid deletedBy)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(new { role_claim_id = roleClaimId, deleted_by = deletedBy }, _jsonSettings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "delete_role_claim",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo eliminar el rol de reclamación", false);
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<RoleClaimVm>> GetAsync(int roleId)
    {
        try
        {
            var filter = JsonConvert.SerializeObject(new { role_id = roleId }, _jsonSettings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_role_claims",
                new { filter },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<RoleClaimVm>("No se encontró el rol de reclamación", false);
            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<RoleClaimDto>>(result);
            var roleClaim = wrapper?.Data?.FirstOrDefault();
            if (roleClaim == null)
                return new Response<RoleClaimVm>("No se encontró el rol de reclamación", false);
            var vm = _mapper.Map<RoleClaimVm>(roleClaim);
            return new Response<RoleClaimVm>(vm, "Operación exitosa");
        }
        catch (Exception ex)
        {
            return new Response<RoleClaimVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<List<RoleClaimVm>>> ListAsync(int? pageNumber, int? pageSize, int? roleId, bool? isActive)
    {
        if (pageNumber == null || pageNumber <= 0)
            pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100)
            pageSize = 50;
        try
        {
            var filter = JsonConvert.SerializeObject(new { role_id = roleId, is_active = isActive }, _jsonSettings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_role_claims",
                new { page_number = pageNumber, page_size = pageSize, filter },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<List<RoleClaimVm>>([], "No se encontraron roles de permisos");

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<RoleClaimDto>>(result);
            var roleClaims = wrapper?.Data;
            var vmList = _mapper.Map<List<RoleClaimVm>>(roleClaims);
            return new Response<List<RoleClaimVm>>(vmList, "Operación exitosa");
        }
        catch (Exception ex)
        {
            return new Response<List<RoleClaimVm>>($"Error: {ex.Message}", false);
        }
    }
}
