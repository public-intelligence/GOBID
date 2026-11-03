using AutoMapper;
using Dapper;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Data;
using GOBID.Application.ViewModels.UserRole;
using GOBID.Application.DTOs.UserRole;
using GOBID.Application.DTOs;

namespace GOBID.Infrastructure.Services;

public class UserRoleService: IUserRoleService
{
    private readonly IDbConnection _connection;
    private readonly IMapper _mapper;
    private readonly IRoleService _roleService;

    public UserRoleService( IMapper mapper, IDbConnection connection, IRoleService roleService)
    {
        _mapper = mapper;
        _connection = connection;
        _roleService = roleService;
    }

    public async Task<Response<string>> DeleteAsync(int userRoleId, Guid updatedBy)
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
            var payload = JsonConvert.SerializeObject(new { user_role_id = userRoleId, updated_by = updatedBy }, settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "delete_user_role",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo eliminar el rol del usuario", false);
            return new Response<string>("Rol eliminado exitosamente del usuario", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al eliminar el rol del usuario: {ex.Message}", false);
        }
    }

    public async Task<Response<UserRoleVm>> GetByIdAsync(int userRoleId)
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
            var payload =  JsonConvert.SerializeObject(new { user_role_id = userRoleId}, settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_user_roles",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<UserRoleVm>("No se encontró el rol del usuario", false);
            var userRoleDto = JsonConvert.DeserializeObject<ResultWrapper<UserRoleDto>>(result);
            if (userRoleDto == null)
                return new Response<UserRoleVm>("No se pudo deserializar el rol del usuario", false);
            var userRole = userRoleDto.Data?.FirstOrDefault();

            var userRoleVm = _mapper.Map<UserRoleVm>(userRole);

            return new Response<UserRoleVm>(userRoleVm, "OK");
        }
        catch (Exception ex)
        {
            return new Response<UserRoleVm>($"Error al obtener el rol del usuario: {ex.Message}", false);
        }
    }

    public async Task<Response<List<UserRoleVm>>> ListAsync(int? pageNumber, int? pageSize, int? userRoleId, Guid? userApp, int? roleId, bool? isActive)
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
            var filterJson = JsonConvert.SerializeObject(new 
            {
                user_role_id = userRoleId,
                user_application_id = userApp,
                role_id = roleId,
                is_active = isActive
            }, settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_user_roles",
                new { payload = filterJson },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result) || result == "{}")
                return new Response<List<UserRoleVm>>([], "No se encontraron roles de usuario");
            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<UserRoleDto>>(result);
            if (wrapper == null || !(wrapper.Data?.Any() ?? false))
                return new Response<List<UserRoleVm>>("No se encontraron roles de usuario", false);
            var userRolesVm = _mapper.Map<List<UserRoleVm>>(wrapper.Data);
            return new Response<List<UserRoleVm>>(userRolesVm, "OK");
        }
        catch (Exception ex)
        {
            return new Response<List<UserRoleVm>>($"Error al listar roles de usuario: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> UpsertAsync(UserRoleUpsertDto dto)
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
                "upsert_user_role",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo insertar/actualizar el rol del usuario", false);
            return new Response<string>("Rol del usuario insertado/actualizado exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al insertar/actualizar el rol del usuario: {ex.Message}", false);
        }
    }
}
