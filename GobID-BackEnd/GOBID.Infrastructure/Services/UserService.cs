using AutoMapper;
using Dapper;
using GOBID.Application.DTOs;
using GOBID.Application.DTOs.Auth;
using GOBID.Application.DTOs.OrganizationApplication;
using GOBID.Application.DTOs.Personnel;
using GOBID.Application.DTOs.User;
using GOBID.Application.DTOs.UserApplication;
using GOBID.Application.DTOs.Pds;
using GOBID.Application.DTOs.UserApplicationPolicy;
using GOBID.Application.DTOs.UserRole;
using GOBID.Application.Enums;
using GOBID.Application.Interfaces;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.User;
using GOBID.Application.ViewModels.UserApplication;
using GOBID.Application.ViewModels.UserRole;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using GOBID.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;
using System.Security.Cryptography;

namespace GOBID.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IDbConnection _connection;
    private readonly IMapper _mapper;
    private readonly ISecondaryDbConnection _secondaryDbConnection;
    private readonly IUserApplicationService _userApplicationService;
    private readonly IUserRoleService _userRoleService;
    private readonly IOrganizationApplicationService _organizationApplicationService;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly IPersonApiService _personApiService;
    private readonly PrincipalContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserApplicationPolicyService _userApplicationPolicyService;

    public UserService(IDbConnection connection, IMapper mapper, ISecondaryDbConnection secondaryDb, IUserApplicationService userApplicationService,
        IUserRoleService userRoleService, IOrganizationApplicationService organizationApplicationService, IConfiguration configuration, IEmailService emailService,
        IPersonApiService personApiService, PrincipalContext context, IServiceProvider serviceProvider, IUserApplicationPolicyService userApplicationPolicyService)
    {
        _connection = connection;
        _mapper = mapper;
        _secondaryDbConnection = secondaryDb;
        _userApplicationService = userApplicationService;
        _userRoleService = userRoleService;
        _organizationApplicationService = organizationApplicationService;
        _config = configuration;
        _emailService = emailService;
        _personApiService = personApiService;
        _context = context;
        _serviceProvider = serviceProvider;
        _userApplicationPolicyService = userApplicationPolicyService;
    }

    public async Task<Response<string>> Upsert(UserUpsertDto dto, Guid? applicationId)
    {
        try
        {

            var personnelExists = await _context.Users
                .AnyAsync(u => u.PersonnelId == dto.PersonnelId && u.IsActive && dto.UserId == null);
            if (personnelExists)
                return new Response<string>("La persona ya esta registrada", false);

            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email && u.IsActive && dto.UserId == null);

            if (emailExists)
                return new Response<string>("El correo electrónico ya está registrado", false);

            string? password = null;
            bool isNewUser = !dto.UserId.HasValue || dto.UserId.Value == Guid.Empty;

            if (isNewUser)
            {
                password = GenerateSecureRandomPassword(12);
                dto.UserPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
                dto.IsActive = true;
                dto.IsExternal = false;
            }
            else
            {
                var existingUser = await _context.Users.FindAsync(dto.UserId!.Value);
                dto.IsExternal = existingUser?.IsExternal ?? false;
            }

            var currentUpdatedBy = dto.UpdatedBy ?? (dto.UserId.HasValue && dto.UserId.Value != Guid.Empty ? dto.UserId.Value : (Guid?)null);

            if (dto.PositionId.HasValue && dto.PositionId.Value != Guid.Empty)
            {
                var personnel = await _context.Personnels
                    .FirstOrDefaultAsync(p => p.PersonId == dto.PersonnelId);

                if (personnel == null)
                {
                    personnel = new Personnel
                    {
                        PersonnelId = Guid.NewGuid(),
                        PersonId = dto.PersonnelId,
                        PositionId = dto.PositionId.Value,
                        // PositionNominateId es el cargo requerido; se inicializa con el mismo
                        // valor asignado aquí ya que este flujo solo maneja un cargo por usuario.
                        PositionNominateId = dto.PositionId.Value,
                        OrganizationId = dto.OrganizationId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUpdatedBy
                    };
                    _context.Personnels.Add(personnel);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (personnel.PositionId != dto.PositionId.Value)
                    {
                        personnel.PositionId = dto.PositionId.Value;
                        if (!personnel.PositionNominateId.HasValue)
                            personnel.PositionNominateId = dto.PositionId.Value;
                        personnel.UpdatedAt = DateTime.UtcNow;
                        personnel.UpdatedBy = currentUpdatedBy;
                        _context.Personnels.Update(personnel);
                        await _context.SaveChangesAsync();
                    }
                }

                dto.PersonPositionId = personnel.PersonnelId;
            }

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
            var payload = JsonConvert.SerializeObject(dto, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "upsert_user_account",
                new { payload },
                commandType: CommandType.StoredProcedure);

            // Limpiamos el ChangeTracker para evitar conflictos de concurrencia optimista:
            // el stored procedure ya modificó la fila del usuario directamente en la BD,
            // por lo que cualquier entidad rastreada por EF Core está desactualizada.
            _context.ChangeTracker.Clear();

            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo insertar/actualizar el usuario", false);

            Guid userIdFromDb;

            if (dto.UserId.HasValue && dto.UserId.Value != Guid.Empty)
            {
                userIdFromDb = dto.UserId.Value;
            }
            else
            {
                try
                {
                    var resultObj = JsonConvert.DeserializeObject<dynamic>(result);
                    var userIdStr = (string?)(resultObj?.user_id ?? resultObj?.UserId);
                    if (string.IsNullOrWhiteSpace(userIdStr))
                        return new Response<string>("No se pudo obtener el identificador del usuario", false);

                    userIdFromDb = Guid.Parse(userIdStr);
                }
                catch
                {
                    return new Response<string>("No se pudo obtener el identificador del usuario", false);
                }
            }

            var updatedBy = dto.UpdatedBy ?? userIdFromDb;

            if (dto.PositionId.HasValue && dto.PositionId.Value != Guid.Empty && dto.PersonPositionId.HasValue)
            {
                var userEntity = await _context.Users.FindAsync(userIdFromDb);
                if (userEntity != null && userEntity.PersonPositionId != dto.PersonPositionId.Value)
                {
                    userEntity.PersonPositionId = dto.PersonPositionId.Value;
                    userEntity.UpdatedAt = DateTime.UtcNow;
                    userEntity.UpdatedBy = updatedBy;
                    await _context.SaveChangesAsync();
                }
            }

            var payloadOrg = new
            {
                user_id = userIdFromDb,
                organization_id = dto.OrganizationId,
                position = dto.Position
            };

            var upsertUserOrg = await _connection.QueryFirstOrDefaultAsync<string>(
                "upsert_user_organization",
                new { payload = JsonConvert.SerializeObject(payloadOrg) },
                commandType: CommandType.StoredProcedure);

            var currentAppsResp = await _userApplicationService.ListAsync(
                pageNumber: 1,
                pageSize: 1000,
                filter: new UserApplicationFilterDto
                {
                    UserId = userIdFromDb,
                    IsActive = null
                });

            if (!currentAppsResp.Succeeded)
                return new Response<string>($"No se pudo leer las aplicaciones actuales del usuario: {currentAppsResp.Message}", false);

            var currentApps = currentAppsResp.Data ?? new List<UserApplicationVm>();
            var incomingIds = new HashSet<int>(dto.UserApplications?.Select(x => x.OrganizationApplicationId) ?? Enumerable.Empty<int>());

            // Códigos de aplicación que el administrador local no puede desvincular bajo ninguna circunstancia
            var protectedAppCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "gobIDLocal", "gobId", "APP3"
            };

            var protectedOrgAppIds = new HashSet<int>();
            if (applicationId.HasValue)
            {
                var orgAppsResp = await _organizationApplicationService.ListAsync(
                    pageNumber: 1,
                    pageSize: 1000,
                    filter: new OrganizationApplicationfilterDto
                    {
                        OrganizationId = dto.OrganizationId,
                        ApplicationId = applicationId.Value
                    });

                if (orgAppsResp.Succeeded && orgAppsResp.Data != null)
                {
                    foreach (var oa in orgAppsResp.Data)
                        protectedOrgAppIds.Add(oa.OrganizationApplicationId);
                }

                if (protectedOrgAppIds.Count == 0)
                {
                    foreach (var a in currentApps)
                    {
                        if (a.OrganizationApplication?.ApplicationId == applicationId.Value)
                            protectedOrgAppIds.Add(a.OrganizationApplicationId);
                    }
                }
            }

            var currentAppIds = new HashSet<int>(currentApps.Select(a => a.OrganizationApplicationId));
            bool applicationsChanged = !currentAppIds.SetEquals(incomingIds);

            var toDelete = currentApps
                .Where(a =>
                    !incomingIds.Contains(a.OrganizationApplicationId) &&
                    !protectedOrgAppIds.Contains(a.OrganizationApplicationId) &&
                    // Si es admin local: no eliminar apps con código protegido
                    !(applicationId.HasValue &&
                      a.OrganizationApplication?.Application?.Code != null &&
                      protectedAppCodes.Contains(a.OrganizationApplication.Application.Code)) &&
                    // Si es admin local: solo puede desvincular OrgApps de su propia organización
                    !(applicationId.HasValue &&
                      a.OrganizationApplication?.OrganizationId != dto.OrganizationId))
                .ToList();

            foreach (var existing in toDelete)
            {
                var rolesResp = await _userRoleService.ListAsync(
                    pageNumber: 1,
                    pageSize: 1000,
                    userRoleId: null,
                    userApp: existing.UserApplicationId,
                    roleId: null,
                    isActive: null);

                if (rolesResp.Succeeded && rolesResp.Data != null)
                {
                    foreach (var r in rolesResp.Data)
                    {
                        var delRoleResp = await _userRoleService.DeleteAsync(r.UserRoleId, updatedBy);
                        if (!delRoleResp.Succeeded)
                            return new Response<string>($"No se pudo eliminar el rol {r.UserRoleId} asociado a la aplicación del usuario: {delRoleResp.Message}", false);
                    }
                }

                var delUaResp = await _userApplicationService.DeleteAsync(existing.UserApplicationId, updatedBy);
                if (!delUaResp.Succeeded)
                    return new Response<string>($"No se pudo eliminar la aplicación del usuario (OrgAppId={existing.OrganizationApplicationId}): {delUaResp.Message}", false);
            }

            if (dto.UserApplications != null && dto.UserApplications.Length > 0)
            {
                var existingAppsDict = currentApps
                    .Where(a => incomingIds.Contains(a.OrganizationApplicationId))
                    .ToDictionary(a => a.OrganizationApplicationId, a => a);

                foreach (var ua in dto.UserApplications)
                {
                    ua.UserId = userIdFromDb;

                    var upsertUaResp = await _userApplicationService.UpsertAsync(ua);
                    if (!upsertUaResp.Succeeded)
                        return new Response<string>($"Error al vincular la aplicación {ua.OrganizationApplicationId}: {upsertUaResp.Message}", false);

                    // Resolver el UserApplicationVm (necesario para roles y políticas)
                    UserApplicationVm? createdUa = null;
                    if (existingAppsDict.TryGetValue(ua.OrganizationApplicationId, out var existingApp))
                    {
                        createdUa = existingApp;
                    }
                    else
                    {
                        var listUaResp = await _userApplicationService.ListAsync(
                            pageNumber: 1,
                            pageSize: 1,
                            filter: new UserApplicationFilterDto
                            {
                                UserId = userIdFromDb,
                                OrganizationApplicationId = ua.OrganizationApplicationId,
                                IsActive = true
                            });

                        if (!listUaResp.Succeeded || listUaResp.Data == null || listUaResp.Data.Count == 0)
                            return new Response<string>($"No se pudo obtener la vinculación de usuario-aplicación para la aplicación {ua.OrganizationApplicationId}", false);

                        createdUa = listUaResp.Data!.First();
                    }

                    if (ua.RoleId.HasValue)
                    {
                        var roleUpdatedBy = ua.UpdatedBy ?? dto.UpdatedBy ?? userIdFromDb;

                        var rolesResp = await _userRoleService.ListAsync(
                            pageNumber: 1,
                            pageSize: 1000,
                            userRoleId: null,
                            userApp: createdUa.UserApplicationId,
                            roleId: null,
                            isActive: true);

                        var currentRoles = rolesResp.Succeeded && rolesResp.Data != null
                            ? rolesResp.Data
                            : new List<UserRoleVm>();

                        var hasDesiredRole = currentRoles.Any(r => r.RoleId == ua.RoleId.Value);
                        if (!hasDesiredRole)
                        {
                            var userRoleDto = new UserRoleUpsertDto
                            {
                                UserApplicationId = createdUa.UserApplicationId,
                                RoleId = ua.RoleId.Value,
                                UpdatedBy = roleUpdatedBy,
                                UserRoleDescription = ua.UserApplicationDescription
                            };

                            var upsertUrResp = await _userRoleService.UpsertAsync(userRoleDto);
                            if (!upsertUrResp.Succeeded)
                                return new Response<string>($"Error al vincular el rol {ua.RoleId.Value} para la aplicación {ua.OrganizationApplicationId}: {upsertUrResp.Message}", false);
                        }

                        foreach (var r in currentRoles.Where(r => r.RoleId != ua.RoleId.Value))
                        {
                            var delRoleResp = await _userRoleService.DeleteAsync(r.UserRoleId, roleUpdatedBy);
                            if (!delRoleResp.Succeeded)
                                return new Response<string>($"No se pudo eliminar el rol {r.UserRoleId} (antiguo) de la aplicación {ua.OrganizationApplicationId}: {delRoleResp.Message}", false);
                        }
                    }

                    // ── Sincronizar políticas ABAC ─────────────────────────────
                    if (ua.PolicyIds != null)
                    {
                        var syncResult = await SyncUserApplicationPoliciesAsync(
                            createdUa.UserApplicationId, ua.PolicyIds, updatedBy);
                        if (!syncResult.Succeeded)
                            return new Response<string>($"Error al sincronizar políticas para la aplicación {ua.OrganizationApplicationId}: {syncResult.Message}", false);
                    }
                    // ──────────────────────────────────────────────────────────
                }
            }

            // Optimización: Solo consultar aplicaciones si es necesario enviar correo
            if (isNewUser || applicationsChanged)
            {
                List<(string redirectUrl, string appName)>? userApplications = null;

                // Reutilizar consulta anterior si no hubo cambios significativos
                // O hacer una nueva consulta solo con aplicaciones activas
                var finalAppsResp = await _userApplicationService.ListAsync(
                    pageNumber: 1,
                    pageSize: 1000,
                    filter: new UserApplicationFilterDto
                    {
                        UserId = userIdFromDb,
                        IsActive = true
                    });

                if (finalAppsResp.Succeeded && finalAppsResp.Data != null && finalAppsResp.Data.Any())
                {
                    userApplications = finalAppsResp.Data
                        .Where(app => app.OrganizationApplication != null)
                        .Select(app => (
                            redirectUrl: app.OrganizationApplication!.Application?.RedirectUrl ?? string.Empty,
                            appName: app.OrganizationApplication.Application?.ApplicationName
                                     ?? app.OrganizationApplication.OrganizationName
                                     ?? "Aplicación"
                        ))
                        .ToList();
                }

                // Solo enviar el correo si hay aplicaciones para mostrar o es un usuario nuevo
                if (isNewUser || (userApplications != null && userApplications.Any()))
                {
                    // Si es nuevo usuario, enviar con contraseña visible
                    // Si es usuario existente, NO mostrar contraseña
                    var emailPassword = password ?? string.Empty;

                    await _emailService.SendCredentialsEmailAsync(
                        dto.Email!,
                        dto.Email!,
                        emailPassword,
                        userApplications,
                        showPassword: isNewUser  // Solo mostrar contraseña si es nuevo usuario
                    );
                }
            }

            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al insertar/actualizar el usuario: {ex.Message}", false);
        }
    }


    public async Task<Response<string>> UpdateUserStatusAsync(UserStatusUpdateDto dto)
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
                "update_user_status",
                new { payload },
                commandType: CommandType.StoredProcedure);

            return new Response<string>("Usuario actualizado exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al actualizar el usuario: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> Delete(Guid userId, Guid deleteBy, string? observation)
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
            var payload = JsonConvert.SerializeObject(new
            {
                user_id = userId,
                deleted_by = deleteBy,
                observation
            }, settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "delete_user_account",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo eliminar el usuario", false);

            return new Response<string>("Usuario eliminado exitosamente", true);
        }
        catch (Exception)
        {
            return new Response<string>("Error", false);
        }
    }

    public async Task<PagedResponse<UserVm>> ListAsync(int? pageNumber, int? pageSize, UserFilterDto? userFilter, int? organizationId)
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

            var filterObj = userFilter ?? new UserFilterDto();

            var payloadObj = new
            {
                page_number = pageNumber,
                page_size = pageSize,
                filter = organizationId.HasValue
                    ? new
                    {
                        organization_id = organizationId.Value,
                        user_id = filterObj.UserId,
                        email_like = filterObj.EmailLike,
                        is_active = filterObj.IsActive
                    }
                    : (object)filterObj
            };

            var payload = JsonConvert.SerializeObject(payloadObj, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_user_accounts",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result) || result == "{}")
                return new PagedResponse<UserVm>([], "Sin resultados");

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<UserDto>>(result);
            var dtos = wrapper?.Data ?? new List<UserDto>();
            var vms = _mapper.Map<List<UserVm>>(dtos);

            var personPositionIds = vms
                .Select(vm => vm.PersonPositionId)
                .Where(id => id.HasValue && id.Value != Guid.Empty)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var personnelMap = new Dictionary<Guid, Personnel>();
            var positionsMap = new Dictionary<Guid, PdsPositionDto>();

            if (personPositionIds.Count > 0)
            {
                var personnelList = await _context.Personnels
                    .Where(p => personPositionIds.Contains(p.PersonnelId))
                    .ToListAsync();

                personnelMap = personnelList.ToDictionary(p => p.PersonnelId);

                var pdsApiService = _serviceProvider.GetRequiredService<IPdsApiService>();
                var orgGroups = personnelList.GroupBy(p => p.OrganizationId);

                foreach (var group in orgGroups)
                {
                    var orgId = group.Key;
                    var posIds = group.Select(p => p.PositionId ?? p.PositionNominateId)
                        .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
                    if (posIds.Count > 0)
                    {
                        var posResult = await pdsApiService.GetPositionsByIdsAsync(orgId, posIds);
                        if (posResult.Succeeded && posResult.Data != null)
                        {
                            foreach (var pos in posResult.Data)
                            {
                                positionsMap[pos.Id] = pos;
                            }
                        }
                    }
                }
            }

            foreach (var vm in vms)
            {
                if (vm.PersonnelId != Guid.Empty)
                {
                    var personnelResult = await _personApiService.GetPersonByIdAsync(vm.PersonnelId);
                    vm.Personnel = personnelResult.Succeeded ? personnelResult.Data : null;
                }

                if (vm.PersonPositionId.HasValue && personnelMap.TryGetValue(vm.PersonPositionId.Value, out var personnel))
                {
                    var effectivePositionId = personnel.PositionId ?? personnel.PositionNominateId;
                    vm.PositionId = effectivePositionId;
                    if (effectivePositionId.HasValue && positionsMap.TryGetValue(effectivePositionId.Value, out var position))
                    {
                        vm.Position = position.Name;
                    }
                }

                if (!vm.OrganizationId.HasValue && organizationId.HasValue)
                {
                    vm.OrganizationId = organizationId.Value;
                }
                else if (!vm.OrganizationId.HasValue)
                {
                    try
                    {
                        var userOrg = await GetUserOrganizationByUserIdAsync(vm.UserId);
                        vm.OrganizationId = userOrg.Succeeded && userOrg.Data != null
                            ? userOrg.Data.OrganizationId
                            : (int?)null;
                    }
                    catch
                    {
                        vm.OrganizationId = null;
                    }
                }
            }

            return new PagedResponse<UserVm>(
                vms,
                wrapper?.CurrentPage ?? pageNumber.Value,
                pageSize.Value,
                wrapper?.TotalTally ?? vms.Count,
                wrapper?.TotalPages ?? 1
            );
        }
        catch (Exception ex)
        {
            return new PagedResponse<UserVm>(new List<UserVm>(),
                "Error al listar los usuarios",
                new List<string> { ex.Message }
            );
        }
    }

    public async Task<Response<UserOrganizationDto>> GetUserOrganizationByUserIdAsync(Guid userId)
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
            var serializedUserId = JsonConvert.SerializeObject(userId, settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_user_organizations",
                new { userId },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<UserOrganizationDto>("No se encontraron datos", false);
            var dtos = JsonConvert.DeserializeObject<List<UserOrganizationDto>>(result);
            var dto = dtos?.FirstOrDefault();
            if (dto == null)
                return new Response<UserOrganizationDto>("No se pudieron deserializar los datos", false);
            return new Response<UserOrganizationDto>(dto, "OK");
        }
        catch (Exception ex)
        {
            return new Response<UserOrganizationDto>($"Error al obtener la organización del usuario: {ex.Message}", false);
        }
    }

    public async Task<Response<UserOrganizationDto>> GetUserByOrgAppIdAsync(int orgAppId)
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

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_user_by_org_app_id",
                new { organization_application_id = orgAppId },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result))
                return new Response<UserOrganizationDto>("No se encontraron datos", false);

            var dto = JsonConvert.DeserializeObject<UserOrganizationDto>(result, settings);
            if (dto == null)
                return new Response<UserOrganizationDto>("No se pudieron deserializar los datos", false);

            return new Response<UserOrganizationDto>(dto, "OK");
        }
        catch (Exception ex)
        {
            return new Response<UserOrganizationDto>($"Error al obtener el usuario por organización-aplicación: {ex.Message}", false);
        }
    }

    public Task<bool> IsApplicationInOrganizationAsync(Guid applicationId, int organizationId) =>
        _context.OrganizationApplications.AnyAsync(oa =>
            oa.ApplicationId == applicationId && oa.OrganizationId == organizationId && oa.IsActive);

    public async Task<PagedResponse<UserVm>> ListUsersByOrganizationAsync(int organizationId, int? pageNumber, int? pageSize, UserOrganizationFilterDto? filter)
    {
        if (pageNumber == null || pageNumber <= 0)
            pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100)
            pageSize = 20;

        try
        {
            var query = _context.UserOrganizations
                .Where(uo => uo.OrganizationId == organizationId)
                .Join(_context.Users,
                    uo => uo.UserId,
                    u => u.UserId,
                    (uo, u) => new { UserOrg = uo, User = u })
                .Where(x => x.User.IsActive && x.User.DischargeDate == null );

            if (!string.IsNullOrWhiteSpace(filter?.Email))
                query = query.Where(x => x.User.Email.Contains(filter.Email));

            if (filter?.RoleId != null)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == filter.RoleId);

                if (role?.PositionId != null && role.PositionId != Guid.Empty)
                {
                    // El rol tiene un cargo asociado en PDS: se buscan los usuarios cuyo Personnel
                    // (via User.PersonPositionId -> Personnel.PersonnelId) ocupa ese cargo.
                    var positionPersonnelIds = await _context.Personnels
                        .Where(p => (p.PositionId ?? p.PositionNominateId) == role.PositionId.Value)
                        .Select(p => p.PersonnelId)
                        .ToListAsync();

                    query = query.Where(x => x.User.PersonPositionId.HasValue
                        && positionPersonnelIds.Contains(x.User.PersonPositionId.Value));
                }
                else
                {
                    query = query.Where(x => _context.UserApplications
                        .Where(ua => ua.UserId == x.User.UserId && ua.IsActive)
                        .Any(ua => ua.UserRoles.Any(ur => ur.RoleId == filter.RoleId && ur.IsActive)));
                }
            }

            if (filter?.PositionId != null)
            {
                var positionPersonnelIds = await _context.Personnels
                    .Where(p => (p.PositionId ?? p.PositionNominateId) == filter.PositionId.Value)
                    .Select(p => p.PersonnelId)
                    .ToListAsync();

                query = query.Where(x => x.User.PersonPositionId.HasValue
                    && positionPersonnelIds.Contains(x.User.PersonPositionId.Value));
            }

            if (filter?.OrganizationalStructureId != null)
            {
                var pdsApiServiceForFilter = _serviceProvider.GetRequiredService<IPdsApiService>();
                // G4: IncludeDescendants opcional (false por defecto, para no cambiar a quien ya usa el filtro).
                var structurePositionIdsResult = await pdsApiServiceForFilter.GetPositionIdsByStructureAsync(
                    organizationId, filter.OrganizationalStructureId.Value, includeDescendants: filter.IncludeDescendants);

                var structurePositionIds = structurePositionIdsResult.Succeeded && structurePositionIdsResult.Data != null
                    ? structurePositionIdsResult.Data.ToHashSet()
                    : [];

                var structurePersonnelIds = await _context.Personnels
                    .Where(p => (p.PositionId ?? p.PositionNominateId).HasValue
                        && structurePositionIds.Contains((p.PositionId ?? p.PositionNominateId)!.Value))
                    .Select(p => p.PersonnelId)
                    .ToListAsync();

                query = query.Where(x => x.User.PersonPositionId.HasValue
                    && structurePersonnelIds.Contains(x.User.PersonPositionId.Value));
            }

            var rawList = await query
                .Select(x => new UserVm
                {
                    UserId = x.User.UserId,
                    PersonnelId = x.User.PersonnelId,
                    Email = x.User.Email,
                    IsActive = x.User.IsActive,
                    MustChangePassword = x.User.MustChangePassword,
                    AdmissionDate = x.User.AdmissionDate,
                    DischargeDate = x.User.DischargeDate,
                    CreatedAt = x.User.CreatedAt,
                    UpdatedAt = x.User.UpdatedAt,
                    OrganizationId = organizationId,
                    Position = x.UserOrg.Position,
                    PersonPositionId = x.User.PersonPositionId
                })
                .ToListAsync();

            // Una sola llamada bulk en lugar de N llamadas individuales
            var personnelMap = new Dictionary<Guid, IdentityPersonResponseDTO>();

            if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
            {
                // Con término de búsqueda: usar el endpoint de search y cruzar con los IDs de la org
                var orgPersonnelIds = rawList
                    .Where(vm => vm.PersonnelId != Guid.Empty)
                    .Select(vm => vm.PersonnelId)
                    .ToHashSet();

                // G3: en lotes de 100 ids (máximo de Identity Hub). Antes se pedía una sola página del
                // tamaño de la organización y, con más de 100 personas, solo se consideraban 50.
                var searchTerm = filter.SearchTerm.Trim();
                personnelMap = await PersonSearchBatching.SearchAllAsync(
                    orgPersonnelIds,
                    batch => _personApiService.SearchPersonsAsync(
                        pageNumber: 1,
                        pageSize: batch.Count,
                        searchTerm: searchTerm,
                        ids: batch));

                // Reducir rawList solo a los usuarios cuyos datos de persona coinciden
                rawList = rawList
                    .Where(vm => personnelMap.ContainsKey(vm.PersonnelId))
                    .ToList();
            }
            else
            {
                // Sin término: bulk fetch de todos los PersonnelIds de la org
                var personnelIds = rawList
                    .Where(vm => vm.PersonnelId != Guid.Empty)
                    .Select(vm => vm.PersonnelId)
                    .Distinct();

                var bulkResult = await _personApiService.GetPersonsByIdsAsync(personnelIds);
                if (bulkResult.Succeeded && bulkResult.Data != null)
                    personnelMap = bulkResult.Data.ToDictionary(p => p.Id);
            }

            foreach (var vm in rawList)
                vm.Personnel = personnelMap.TryGetValue(vm.PersonnelId, out var p) ? p : null;

            var totalCount = rawList.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

            var paged = rawList
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();

            // Resolver Posición y Localidad desde la tabla Personnel (User.PersonPositionId -> Personnel.PersonnelId)
            var personPositionIds = paged
                .Where(vm => vm.PersonPositionId.HasValue && vm.PersonPositionId.Value != Guid.Empty)
                .Select(vm => vm.PersonPositionId!.Value)
                .Distinct()
                .ToList();

            if (personPositionIds.Count > 0)
            {
                var personnelList = await _context.Personnels
                    .Where(p => personPositionIds.Contains(p.PersonnelId))
                    .ToListAsync();

                var personnelByPositionMap = personnelList.ToDictionary(p => p.PersonnelId);

                var positionIds = personnelList
                    .Select(p => p.PositionId ?? p.PositionNominateId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                var localityIds = personnelList
                    .Where(p => p.LocalityId.HasValue)
                    .Select(p => p.LocalityId!.Value)
                    .Distinct()
                    .ToList();

                var pdsApiService = _serviceProvider.GetRequiredService<IPdsApiService>();

                var positionsMap = new Dictionary<Guid, PdsPositionDto>();
                if (positionIds.Count > 0)
                {
                    var posResult = await pdsApiService.GetPositionsByIdsAsync(organizationId, positionIds);
                    if (posResult.Succeeded && posResult.Data != null)
                        positionsMap = posResult.Data.ToDictionary(p => p.Id);
                }

                var localitiesMap = new Dictionary<Guid, PdsLocalidadDto>();
                if (localityIds.Count > 0)
                {
                    var localidadResult = await pdsApiService.GetLocalidadesByIdsAsync(localityIds);
                    if (localidadResult.Succeeded && localidadResult.Data != null)
                        localitiesMap = localidadResult.Data.ToDictionary(l => l.Id);
                }

                foreach (var vm in paged)
                {
                    if (!vm.PersonPositionId.HasValue || !personnelByPositionMap.TryGetValue(vm.PersonPositionId.Value, out var personnel))
                        continue;

                    var effectivePositionId = personnel.PositionId ?? personnel.PositionNominateId;
                    vm.PositionId = effectivePositionId;
                    if (effectivePositionId.HasValue && positionsMap.TryGetValue(effectivePositionId.Value, out var position))
                    {
                        vm.Position = position.Name;
                        vm.OrganizationalStructure = position.OrganizationalStructure;
                    }

                    if (personnel.LocalityId.HasValue && localitiesMap.TryGetValue(personnel.LocalityId.Value, out var locality))
                        vm.Locality = locality;
                }
            }

            return new PagedResponse<UserVm>(paged, pageNumber.Value, pageSize.Value, totalCount, Math.Max(totalPages, 1));
        }
        catch (Exception ex)
        {
            return new PagedResponse<UserVm>(
                new List<UserVm>(),
                $"Error al listar los usuarios de la organización: {ex.Message}",
                new List<string> { ex.Message }
            );
        }
    }

    public async Task<PagedResponse<UserWithPersonnelDto>> ListUsersByOrganizationApplicationAsync(int organizationApplicationId, int? pageNumber, int? pageSize)
    {
        if (pageNumber == null || pageNumber <= 0)
            pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100)
            pageSize = 50;

        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_personnel_by_organization_application",
                new
                {
                    organization_application_id = organizationApplicationId,
                    page_number = pageNumber,
                    page_size = pageSize
                },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result) || result == "{}")
                return new PagedResponse<UserWithPersonnelDto>(new List<UserWithPersonnelDto>(), "Sin resultados");

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            // El SP devuelve meta y data como strings JSON escapados, requiere dos pasos
            var jsonResponse = JsonConvert.DeserializeObject<dynamic>(result);

            int totalTally = 0;
            int currentPage = pageNumber.Value;
            int totalPages = 1;

            if (jsonResponse?.meta != null)
            {
                var meta = JsonConvert.DeserializeObject<MetaData>(jsonResponse.meta.ToString(), settings);
                if (meta != null)
                {
                    totalTally = meta.TotalTally;
                    currentPage = meta.CurrentPage;
                    totalPages = meta.TotalPages;
                }
            }

            var users = new List<UserWithPersonnelDto>();
            if (jsonResponse?.data != null)
                users = JsonConvert.DeserializeObject<List<UserWithPersonnelDto>>(jsonResponse.data.ToString(), settings) ?? new List<UserWithPersonnelDto>();

            var usersToFetch = users.Where(u => u.PersonnelId != Guid.Empty).ToList();
            if (usersToFetch.Count > 0)
            {
                var tasks = usersToFetch
                    .Select(u => _personApiService.GetPersonByIdAsync(u.PersonnelId))
                    .ToList();
                var personnelResults = await Task.WhenAll(tasks);
                for (int i = 0; i < usersToFetch.Count; i++)
                {
                    if (personnelResults[i].Succeeded && personnelResults[i].Data != null)
                        usersToFetch[i].Personnel = personnelResults[i].Data;
                }
            }

            return new PagedResponse<UserWithPersonnelDto>(
                users,
                currentPage,
                pageSize.Value,
                totalTally,
                totalPages
            );
        }
        catch (SqlException ex) when (ex.Number == 51000)
        {
            try
            {
                var errorData = JsonConvert.DeserializeObject<dynamic>(ex.Message);
                var errorMessage = (string)(errorData?.message ?? "Error desconocido");
                return new PagedResponse<UserWithPersonnelDto>(
                    new List<UserWithPersonnelDto>(),
                    errorMessage,
                    new List<string> { errorMessage }
                );
            }
            catch
            {
                return new PagedResponse<UserWithPersonnelDto>(
                    new List<UserWithPersonnelDto>(),
                    $"Error SQL: {ex.Message}",
                    new List<string> { ex.Message }
                );
            }
        }
        catch (Exception ex)
        {
            return new PagedResponse<UserWithPersonnelDto>(
                new List<UserWithPersonnelDto>(),
                $"Error al listar los usuarios: {ex.Message}",
                new List<string> { ex.Message }
            );
        }
    }

    public async Task<PagedResponse<UserWithPersonnelDto>> GetUsersByOrganizationApplicationWithPositionAsync(
        int organizationApplicationId, string? search, int? pageNumber, int? pageSize)
    {
        if (pageNumber == null || pageNumber <= 0)
            pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100)
            pageSize = 50;

        bool hasFilter = !string.IsNullOrWhiteSpace(search);

        try
        {
            var projectedQuery = _context.UserApplications
                .Where(ua => ua.OrganizationApplicationId == organizationApplicationId && ua.IsActive)
                .Join(_context.Users,
                    ua => ua.UserId,
                    u => u.UserId,
                    (ua, u) => new { UserApp = ua, User = u })
                .Where(x => x.User.IsActive)
                .OrderBy(x => x.User.Email)
                .Select(x => new UserWithPersonnelDto
                {
                    PersonnelId = x.User.PersonnelId,
                    UserId = x.User.UserId,
                    Email = x.User.Email,
                    UserApplicationId = x.UserApp.UserApplicationId,
                    OrganizationApplicationId = x.UserApp.OrganizationApplicationId,
                    CreatedAt = x.User.CreatedAt
                });

            List<UserWithPersonnelDto> users;
            int totalCount;
            int totalPages;

            if (hasFilter)
            {
               
                users = await projectedQuery.Take(1000).ToListAsync();

                await AttachPersonnelInfoAsync(users);
                await EnrichUsersWithPositionAndRoleAsync(users);

                var candidateIds = users
                    .Where(u => u.PersonnelId != Guid.Empty)
                    .Select(u => u.PersonnelId)
                    .Distinct()
                    .ToList();

                var matchedByName = new HashSet<Guid>();
                if (candidateIds.Count > 0)
                {
                    var searchResp = await _personApiService.SearchPersonsAsync(1, candidateIds.Count, search!, candidateIds);
                    if (searchResp.Succeeded && searchResp.Data != null)
                        matchedByName = searchResp.Data.Select(p => p.Id).ToHashSet();
                }

                users = users
                    .Where(u =>
                        (u.Position?.Name != null && u.Position.Name.Contains(search!, StringComparison.OrdinalIgnoreCase)) ||
                        matchedByName.Contains(u.PersonnelId))
                    .ToList();

                totalCount = users.Count;
                totalPages = Math.Max((int)Math.Ceiling(totalCount / (double)pageSize.Value), 1);

                users = users
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .ToList();
            }
            else
            {
                totalCount = await projectedQuery.CountAsync();
                if (totalCount == 0)
                    return new PagedResponse<UserWithPersonnelDto>(new List<UserWithPersonnelDto>(), "Sin resultados");

                totalPages = Math.Max((int)Math.Ceiling(totalCount / (double)pageSize.Value), 1);

                users = await projectedQuery
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .ToListAsync();

                await AttachPersonnelInfoAsync(users);
                await EnrichUsersWithPositionAndRoleAsync(users);
            }

            if (users.Count == 0)
                return new PagedResponse<UserWithPersonnelDto>(new List<UserWithPersonnelDto>(), "Sin resultados");

            return new PagedResponse<UserWithPersonnelDto>(
                users,
                pageNumber.Value,
                pageSize.Value,
                totalCount,
                totalPages
            );
        }
        catch (Exception ex)
        {
            return new PagedResponse<UserWithPersonnelDto>(
                new List<UserWithPersonnelDto>(),
                $"Error al listar los usuarios: {ex.Message}",
                new List<string> { ex.Message }
            );
        }
    }

    // Resuelve la info de Persona (Identity) en bulk para un lote de usuarios.
    private async Task AttachPersonnelInfoAsync(List<UserWithPersonnelDto> users)
    {
        var usersToFetch = users.Where(u => u.PersonnelId != Guid.Empty).ToList();
        if (usersToFetch.Count == 0)
            return;

        var tasks = usersToFetch
            .Select(u => _personApiService.GetPersonByIdAsync(u.PersonnelId))
            .ToList();
        var personnelResults = await Task.WhenAll(tasks);
        for (int i = 0; i < usersToFetch.Count; i++)
        {
            if (personnelResults[i].Succeeded && personnelResults[i].Data != null)
                usersToFetch[i].Personnel = personnelResults[i].Data;
        }
    }

    // Resuelve Posición (con su estructura organizacional) y Rol asignado para cada usuario:
    // User.PersonPositionId -> Personnel.PersonnelId -> Personnel.PositionId/PositionNominateId -> PDS
    private async Task EnrichUsersWithPositionAndRoleAsync(List<UserWithPersonnelDto> users)
    {
        if (users.Count == 0)
            return;

        var userIds = users.Select(u => u.UserId).ToList();
        var userEntities = await _context.Users
            .Where(u => userIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.PersonPositionId })
            .ToListAsync();

        var personPositionByUser = userEntities
            .Where(u => u.PersonPositionId.HasValue && u.PersonPositionId.Value != Guid.Empty)
            .ToDictionary(u => u.UserId, u => u.PersonPositionId!.Value);

        var personPositionIds = personPositionByUser.Values.Distinct().ToList();

        if (personPositionIds.Count > 0)
        {
            var personnelList = await _context.Personnels
                .Where(p => personPositionIds.Contains(p.PersonnelId))
                .ToListAsync();

            var personnelMap = personnelList.ToDictionary(p => p.PersonnelId);

            var pdsApiService = _serviceProvider.GetRequiredService<IPdsApiService>();

           
            var positionsMap = new Dictionary<Guid, PdsPositionDto>();
            var orgGroups = personnelList.GroupBy(p => p.OrganizationId);

            foreach (var group in orgGroups)
            {
                var orgId = group.Key;
                var posIds = group.Select(p => p.PositionId ?? p.PositionNominateId)
                    .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
                if (posIds.Count == 0)
                    continue;

                var posResult = await pdsApiService.GetPositionsByIdsAsync(orgId, posIds);
                if (posResult.Succeeded && posResult.Data != null)
                {
                    foreach (var pos in posResult.Data)
                        positionsMap[pos.Id] = pos;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"GetPositionsByIdsAsync falló para org={orgId} posIds=[{string.Join(",", posIds)}]: {posResult.Message}");
                }
            }

            foreach (var user in users)
            {
                if (!personPositionByUser.TryGetValue(user.UserId, out var personPositionId) ||
                    !personnelMap.TryGetValue(personPositionId, out var personnel))
                    continue;

                var effectivePositionId = personnel.PositionId ?? personnel.PositionNominateId;
                user.PositionId = effectivePositionId;
                if (effectivePositionId.HasValue && positionsMap.TryGetValue(effectivePositionId.Value, out var position))
                    user.Position = position;
            }
        }

        foreach (var user in users)
        {
            var rolesResp = await _userRoleService.ListAsync(
                pageNumber: 1,
                pageSize: 1,
                userRoleId: null,
                userApp: user.UserApplicationId,
                roleId: null,
                isActive: true);

            if (rolesResp.Succeeded && rolesResp.Data != null && rolesResp.Data.Count > 0)
            {
                user.RoleId = rolesResp.Data[0].RoleId;
                user.RoleName = rolesResp.Data[0].RoleName;
            }
        }
    }

    private class MetaData
    {
        [JsonProperty("total_tally")]
        public int TotalTally { get; set; }

        [JsonProperty("current_page")]
        public int CurrentPage { get; set; }

        [JsonProperty("total_pages")]
        public int TotalPages { get; set; }
    }

    public async Task<Response<UserVm>> GetByIdAsync(Guid id, int? organizationId = null)
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

            var payloadObj = new
            {
                pageNumber = 1,
                pageSize = 1,
                filter = new { user_id = id }
            };
            var payload = JsonConvert.SerializeObject(payloadObj, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_user_accounts",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result))
                return new Response<UserVm>("Usuario no encontrado", false);

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<UserDto>>(result);
            var userDto = wrapper?.Data?.FirstOrDefault();
            if (userDto == null)
                return new Response<UserVm>("Usuario no encontrado", false);

            var userVm = _mapper.Map<UserVm>(userDto);

            if (userVm.PersonnelId != Guid.Empty)
            {
                var personnelResult = await _personApiService.GetPersonByIdAsync(userVm.PersonnelId);
                userVm.Personnel = personnelResult.Succeeded ? personnelResult.Data : null;
            }

            if (userVm.PersonPositionId.HasValue && userVm.PersonPositionId.Value != Guid.Empty)
            {
                var personnel = await _context.Personnels
                    .FirstOrDefaultAsync(p => p.PersonnelId == userVm.PersonPositionId.Value);

                if (personnel != null)
                {
                    var effectivePositionId = personnel.PositionId ?? personnel.PositionNominateId;
                    userVm.PositionId = effectivePositionId;

                    var pdsApiService = _serviceProvider.GetRequiredService<IPdsApiService>();
                    var posResult = effectivePositionId.HasValue
                        ? await pdsApiService.GetPositionAsync(personnel.OrganizationId, effectivePositionId.Value)
                        : new Response<PdsPositionDto>(null, "Ok");
                    if (posResult.Succeeded && posResult.Data != null)
                    {
                        userVm.Position = posResult.Data.Name;
                    }
                }
            }

            var uaResp = await _userApplicationService.ListAsync(
                pageNumber: 1,
                pageSize: 1000,
                filter: new UserApplicationFilterDto { UserId = userVm.UserId, IsActive = true, OrganizationId = organizationId });

            if (uaResp.Succeeded && uaResp.Data != null && uaResp.Data.Any())
            {
                var upserts = new List<UserApplicationUpsertDto>();
                int? resolvedOrganizationId = organizationId;

                foreach (var uaVm in uaResp.Data)
                {
                    int? roleId = null;
                    string? roleName = null;
                    var rolesResp = await _userRoleService.ListAsync(
                        pageNumber: 1,
                        pageSize: 1,
                        userRoleId: null,
                        userApp: uaVm.UserApplicationId,
                        roleId: null,
                        isActive: true);

                    if (rolesResp.Succeeded && rolesResp.Data != null && rolesResp.Data.Any())
                        roleId = rolesResp.Data.First().RoleId;
                    roleName = rolesResp.Data?.FirstOrDefault()?.RoleName;

                    int? uaOrganizationId = uaVm.OrganizationApplication?.OrganizationId > 0
                        ? uaVm.OrganizationApplication.OrganizationId
                        : null;

                    if (uaOrganizationId is null)
                    {
                        var orgAppResp = await _organizationApplicationService.GetAsync(1, 500, uaVm.OrganizationApplicationId);
                        if (orgAppResp.Succeeded && orgAppResp.Data != null)
                            uaOrganizationId = orgAppResp.Data.OrganizationId;
                    }

                    if (resolvedOrganizationId is null)
                        resolvedOrganizationId = uaOrganizationId;

                    upserts.Add(new UserApplicationUpsertDto
                    {
                        UserApplicationId = uaVm.UserApplicationId,
                        UserId = userVm.UserId,
                        OrganizationApplicationId = uaVm.OrganizationApplicationId,
                        UserApplicationDescription = uaVm.UserApplicationDescription,
                        IsApplicationAdmin = uaVm.IsApplicationAdmin,
                        IsActive = uaVm.IsActive,
                        UpdatedBy = uaVm.UpdatedBy,
                        RowVersion = uaVm.RowVersion,
                        RoleId = roleId,
                        RoleName = roleName,
                        OrganizationId = uaOrganizationId
                    });
                }

                userVm.UserApplications = upserts.ToArray();
                userVm.OrganizationId = resolvedOrganizationId;
            }
            else
            {
                userVm.UserApplications = [];
                userVm.OrganizationId = null;
            }

            return new Response<UserVm>(userVm, "OK");
        }
        catch (Exception ex)
        {
            return new Response<UserVm>($"Error al obtener el usuario: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> UpsertUserOrganization(UserOrganizationUpsertDto dto)
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
                "upsert_user_organization",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo insertar/actualizar la organización del usuario", false);
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al insertar/actualizar la organización del usuario: {ex.Message}", false);
        }
    }

    public async Task<Response<PersonnelDto>> GetPersonnelByIdentificationNumber(string identificationNumber)
    {
        try
        {
            if (_secondaryDbConnection.State != ConnectionState.Open)
                _secondaryDbConnection.Open();

            var json = await _secondaryDbConnection.QueryFirstOrDefaultAsync<string>(
                "sp_GetPersonnelByCedula",
                new { Cedula = identificationNumber },
                commandType: CommandType.StoredProcedure);

            if (!string.IsNullOrWhiteSpace(json))
            {
                var dbPersonnel = JsonConvert.DeserializeObject<PersonnelDto>(json);
                if (dbPersonnel != null)
                    return new Response<PersonnelDto>(dbPersonnel, "OK");


            }

            var baseUrl = _config["ApiSettings:BaseUrl"];
            var token = _config["ApiSettings:AuthorizationToken"];

            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token))
                return new Response<PersonnelDto>("Configuración de API inválida", false);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", token);

            var resp = await client.GetAsync($"{baseUrl}JCE/Info/{identificationNumber}"); 
            if (!resp.IsSuccessStatusCode)
                return new Response<PersonnelDto>($"No se pudo consultar la JCE (HTTP {(int)resp.StatusCode})", false);

            var resultado = await resp.Content.ReadAsStringAsync();
            dynamic? tempResult = JsonConvert.DeserializeObject(resultado);
            var root = tempResult?.root;

            bool jceSuccess = false;
            if (root != null && root!.success != null)
            {
                var s = (string?)Convert.ToString(root!.success);
                jceSuccess = string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "1");
            }

            if (!jceSuccess)
                return new Response<PersonnelDto>("No se encontró una persona con la cédula ingresada en la JCE", false);

            string firstNames = Convert.ToString(root!.nombres) ?? string.Empty;
            string firstSurname = Convert.ToString(root!.apellido1) ?? string.Empty;
            string secondSurname = Convert.ToString(root!.apellido2) ?? string.Empty;

            var jcePersonnel = new PersonnelDto
            {
                IdentificationNumber = identificationNumber,
                FirstName = firstNames.Trim(),
                LastName = $"{firstSurname} {secondSurname}".Trim(),
                Position = string.Empty
            };

            return new Response<PersonnelDto>("El usuario no esta registrado en el SED", false);
        }
        catch (Exception)
        {
            return new Response<PersonnelDto>(null)
            {
                Succeeded = false,
                Message = "No se pudo encontrar una persona con la cedula registrada"
            };
        }
        finally
        {
            if (_secondaryDbConnection.State == ConnectionState.Open)
                _secondaryDbConnection.Close();
        }
    }

    public async Task<Response<PersonnelDto>> GetPersonnelByIdAsync(Guid personnelId)
    {
        try
        {
            if (_secondaryDbConnection.State != ConnectionState.Open)
                _secondaryDbConnection.Open();

            var json = await _secondaryDbConnection.QueryFirstOrDefaultAsync<string>(
                "sp_GetPersonnelByUserId",
                new { UserId = personnelId },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(json))
                return new Response<PersonnelDto>("No se recibieron datos", false);

            var data = JsonConvert.DeserializeObject<PersonnelDto>(json);
            return new Response<PersonnelDto>(data, "OK");
        }
        catch (SqlException ex) when (ex.Number == 51000)
        {
            try
            {
                var errorMessage = JsonConvert.DeserializeObject<string>(ex.Message);
                return new Response<PersonnelDto>(null)
                {
                    Succeeded = false,
                    Message = errorMessage ?? "Error desconocido"
                };
            }
            catch
            {
                return new Response<PersonnelDto>(null)
                {
                    Succeeded = false,
                    Message = $"Error SQL: {ex.Message}"
                };
            }
        }
        catch (Exception ex)
        {
            return new Response<PersonnelDto>(null)
            {
                Succeeded = false,
                Message = $"Error al obtener el personal: {ex.Message}"
            };
        }
        finally
        {
            if (_secondaryDbConnection.State == ConnectionState.Open)
                _secondaryDbConnection.Close();
        }
    }

    public async Task<PagedResponse<UserVm>> ListCountAsync(int? pageNumber, int? pageSize, UserFilterDto? userFilter)
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

            var payloadObj = new
            {
                page_number = pageNumber,
                page_size = pageSize,
                filter = userFilter
            };
            var payload = JsonConvert.SerializeObject(payloadObj, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_user_accounts",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result) || result == "{}")
                return new PagedResponse<UserVm>([], "Sin resultados");

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<UserDto>>(result);
            var dtos = wrapper?.Data ?? new List<UserDto>();
            var vms = _mapper.Map<List<UserVm>>(dtos);

            foreach (var vm in vms)
            {
                if (vm.PersonnelId != Guid.Empty)
                {
                    var personnelResult = await _personApiService.GetPersonByIdAsync(vm.PersonnelId);
                    vm.Personnel = personnelResult.Succeeded ? personnelResult.Data : null;
                }

                var appFilter = new UserApplicationFilterDto { UserId = vm.UserId, IsActive = true };
                var userAppResponse = await _userApplicationService.ListAsync(1, 100, appFilter);
                vm.Count = userAppResponse.Succeeded && userAppResponse.Data != null
                    ? userAppResponse.Data.Count
                    : 0;
            }

            return new PagedResponse<UserVm>(
                vms,
                wrapper?.CurrentPage ?? (pageNumber ?? 1),
                pageSize.Value,
                wrapper?.TotalTally ?? vms.Count,
                wrapper?.TotalPages ?? 1
            );

        }
        catch (Exception)
        {
            return new PagedResponse<UserVm>(new List<UserVm>(),
                "Error al listar los usuarios",
                new List<string> { "error" }
            );
        }
    }

    public async Task<Response<string>> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
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
            if (dto.NewPassword != dto.ConfirmNewPassword)
                return new Response<string>("Las contraseñas nuevas no coinciden", false);
            var user = await GetByIdAsync(userId);
            var payload = JsonConvert.SerializeObject(new
            {
                user_id = userId,
                new_password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword),
                must_change_password = dto.MustChangePassword
            }, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "update_user_password",
                new { payload },
                commandType: CommandType.StoredProcedure);

            return new Response<string>("Contraseña cambiada exitosamente", true);
        }
        catch (Exception)
        {
            return new Response<string>($"Error al cambiar la contraseña", false);
        }
    }

    public async Task<Response<PersonnelInfoDto>> GetPersonnelInfoByIdAsync(Guid personnelId)
    {
        try
        {
            if (_secondaryDbConnection.State != ConnectionState.Open)
                _secondaryDbConnection.Open();

            var json = await _secondaryDbConnection.QueryFirstOrDefaultAsync<string>(
                "sp_GetPersonnelInfo",
                new { UserId = personnelId },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(json))
                return new Response<PersonnelInfoDto>("No se recibieron datos", false);

            var data = JsonConvert.DeserializeObject<PersonnelInfoDto>(json);
            return new Response<PersonnelInfoDto>(data, "OK");
        }

        catch (Exception)
        {
            return new Response<PersonnelInfoDto>(null)
            {
                Succeeded = false,
                Message = "Error al obtener el personal"
            };
        }
        finally
        {
            if (_secondaryDbConnection.State == ConnectionState.Open)
                _secondaryDbConnection.Close();
        }
    }

    public async Task<Response<List<AreaDto>>> GetAreasForOrganizationAsync(Guid areaId)
    {
        try
        {
            if (_secondaryDbConnection.State != ConnectionState.Open)
                _secondaryDbConnection.Open();

            // Ejecuta el SP y mapea directamente a una lista de AreaDto
            var areas = await _secondaryDbConnection.QueryAsync<AreaDto>(
                "sp_GetAreasForOrganization",
                new { AreaId = areaId.ToString() },
                commandType: CommandType.StoredProcedure
            );

            var areaList = areas.ToList();

            if (!areaList.Any())
                return new Response<List<AreaDto>>("No se recibieron datos", false);

            return new Response<List<AreaDto>>(areaList, "OK");
        }
        catch (SqlException ex) when (ex.Number == 51000)
        {
            return new Response<List<AreaDto>>(null)
            {
                Succeeded = false,
                Message = $"Error SQL: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new Response<List<AreaDto>>(null)
            {
                Succeeded = false,
                Message = $"Error al obtener las áreas: {ex.Message}"
            };
        }
        finally
        {
            if (_secondaryDbConnection.State == ConnectionState.Open)
                _secondaryDbConnection.Close();
        }
    }

    /// <summary>
    /// Genera una contraseña aleatoria criptográficamente segura.
    /// </summary>
    /// <param name="length">Longitud de la contraseña (mínimo 12 caracteres recomendado)</param>
    /// <returns>Contraseña segura generada</returns>
    private static string GenerateSecureRandomPassword(int length)
    {
        if (length < 8)
            throw new ArgumentException("La longitud de la contraseña debe ser al menos 8 caracteres", nameof(length));

        // Conjuntos de caracteres para mayor entropía
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string specialChars = "!@#$%&*-_=+";

        // Combinar todos los conjuntos
        const string allChars = lowercase + uppercase + digits + specialChars;

        var password = new char[length];

        using (var rng = RandomNumberGenerator.Create())
        {
            var buffer = new byte[length * 4];
            rng.GetBytes(buffer);

            // Asegurar al menos un carácter de cada tipo
            password[0] = lowercase[GetRandomIndex(buffer, 0, lowercase.Length)];
            password[1] = uppercase[GetRandomIndex(buffer, 1, uppercase.Length)];
            password[2] = digits[GetRandomIndex(buffer, 2, digits.Length)];
            password[3] = specialChars[GetRandomIndex(buffer, 3, specialChars.Length)];

            // Llenar el resto con caracteres aleatorios
            for (int i = 4; i < length; i++)
            {
                password[i] = allChars[GetRandomIndex(buffer, i, allChars.Length)];
            }

            // Mezclar la contraseña usando Fisher-Yates shuffle
            for (int i = length - 1; i > 0; i--)
            {
                int j = GetRandomIndex(buffer, i + length, i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }
        }

        return new string(password);
    }

    /// <summary>
    /// Obtiene un índice aleatorio criptográficamente seguro.
    /// </summary>
    private static int GetRandomIndex(byte[] buffer, int offset, int maxValue)
    {
        var value = BitConverter.ToUInt32(buffer, (offset * 4) % (buffer.Length - 4));
        return (int)(value % (uint)maxValue);
    }

    /// <summary>
    /// Método legacy - mantener por compatibilidad pero marcado como obsoleto
    /// </summary>
    [Obsolete("Usar GenerateSecureRandomPassword en su lugar por razones de seguridad")]
    private static string GenerateRandomPassword(int length)
    {
        const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
        var res = new char[length];
        var rnd = new Random();
        for (int i = 0; i < length; i++)
        {
            res[i] = valid[rnd.Next(valid.Length)];
        }
        return new string(res);
    }

    public async Task<Response<string>> RegisterPublicAsync(PublicRegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
            return new Response<string>("Las contraseñas no coinciden", false);

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == dto.Email && u.IsActive);

        if (emailExists)
            return new Response<string>("El correo electrónico ya está registrado", false);

        var personnelExists = await _context.Users
            .AnyAsync(u => u.PersonnelId == dto.PersonnelId && u.IsActive);

        if (personnelExists)
            return new Response<string>("Ya existe una cuenta registrada con la cédula ingresada", false);

        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        var payload = JsonConvert.SerializeObject(new
        {
            personnel_id = dto.PersonnelId,
            email = dto.Email,
            user_password = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
            must_change_password = false,
            is_active = true,
            is_external = true,
            phone_number = dto.PhoneNumber
        }, settings);

        var result = await _connection.QueryFirstOrDefaultAsync<string>(
            "upsert_user_account",
            new { payload },
            commandType: CommandType.StoredProcedure);

        if (string.IsNullOrEmpty(result))
            return new Response<string>("No se pudo registrar el usuario", false);

        var resultObj = JsonConvert.DeserializeObject<dynamic>(result);
        var userIdStr = (string?)(resultObj?.user_id ?? resultObj?.UserId);

        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return new Response<string>("No se pudo obtener el identificador del usuario", false);

        var authService = _serviceProvider.GetRequiredService<IAuthService>();
        var confirmationToken = await authService.GenerateEmailConfirmationTokenAsync(userId, dto.Email);

        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.EmailConfirmationToken = confirmationToken;
            await _context.SaveChangesAsync();
        }

        var clientBaseUrl = _config["ClientBaseUrl"]?.TrimEnd('/') ?? string.Empty;
        var confirmationLink = $"{clientBaseUrl}/verify/email?token={Uri.EscapeDataString(confirmationToken)}";

        try
        {
            await _emailService.SendEmailConfirmationAsync(dto.Email, dto.Email, confirmationLink);
        }
        catch
        {
        }

        return new Response<string>
        {
            Succeeded = true,
            Message = "Usuario registrado exitosamente. Por favor revisa tu correo para confirmar tu cuenta.",
            Data = userIdStr
        };
    }

    public async Task<Response<ConfirmEmailDto>> ConfirmEmailAsync(ConfirmEmailDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
                return new Response<ConfirmEmailDto>("El token de confirmación es requerido", false);

            var authService = _serviceProvider.GetRequiredService<IAuthService>();
            var validationResult = await authService.ValidateEmailConfirmationTokenAsync(dto.Token);

            if (!validationResult.Succeeded)
                return new Response<ConfirmEmailDto>(validationResult.Message ?? string.Empty, false);

            var confirmPayload = JsonConvert.SerializeObject(new { user_id = validationResult.Data });

            await _connection.QueryFirstOrDefaultAsync<string>(
                "confirm_user_email",
                new { payload = confirmPayload },
                commandType: CommandType.StoredProcedure);

            return new Response<ConfirmEmailDto>("Correo electrónico confirmado exitosamente", true);
        }
        catch (Exception ex)
        {
            return new Response<ConfirmEmailDto>($"Error al confirmar el correo electrónico: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> RegisterPersonAndUserAsync(RegisterPersonAndUserDto dto)
    {
        var createPersonDto = new CreatePersonDTO
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IdentificationNumber = dto.IdentificationNumber,
            Gender = dto.Gender,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Birthdate = dto.Birthdate,
            IsExternal = dto.IsExternal,
            Status = EnumStatus.Active
        };

        var personResponse = await _personApiService.CreatePersonAsync(createPersonDto);

        if (!personResponse.Succeeded)
            return new Response<string>(personResponse.Message, false);

        var registerDto = new PublicRegisterDto
        {
            Email = dto.Email,
            Password = dto.Password,
            ConfirmPassword = dto.ConfirmPassword,
            PersonnelId = personResponse.Data!.Id,
            PhoneNumber = dto.PhoneNumber
        };

        var userResponse = await RegisterPublicAsync(registerDto);

        if (!userResponse.Succeeded)
            return new Response<string>(userResponse.Message, false);

        return new Response<string>
        {
            Succeeded = true,
            Message = "Usuario registrado exitosamente. Por favor revisa tu correo para confirmar tu cuenta.",
            Data = userResponse.Data
        };
    }

    // ── Sincronización de políticas ABAC para un UserApplication ─────────────
    private async Task<Response<string>> SyncUserApplicationPoliciesAsync(
        Guid userApplicationId, List<Guid> incomingPolicyIds, Guid updatedBy)
    {
        try
        {
            // Obtener asignaciones actuales activas
            var current = await _context.Set<UserApplicationPolicy>()
                .Where(x => x.UserApplicationId == userApplicationId && x.IsActive)
                .ToListAsync();

            var currentIds = current.Select(x => x.PolicyId).ToHashSet();
            var incomingSet = incomingPolicyIds.ToHashSet();

            // Desactivar las que ya no están en la lista
            var toRemove = current.Where(x => !incomingSet.Contains(x.PolicyId)).ToList();
            foreach (var item in toRemove)
            {
                item.IsActive = false;
                item.UpdatedAt = DateTime.UtcNow;
                item.UpdatedBy = updatedBy;
            }

            // Agregar las nuevas que no existen
            var toAdd = incomingSet.Where(id => !currentIds.Contains(id)).ToList();
            foreach (var policyId in toAdd)
            {
                _context.Set<GOBID.Domain.Entities.UserApplicationPolicy>().Add(new GOBID.Domain.Entities.UserApplicationPolicy
                {
                    UserApplicationPolicyId = Guid.NewGuid(),
                    UserApplicationId = userApplicationId,
                    PolicyId = policyId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = updatedBy
                });
            }

            await _context.SaveChangesAsync();
            return new Response<string>("Políticas sincronizadas", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al sincronizar políticas: {ex.Message}", false);
        }
    }
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<Response<UserVm>> GetByEmailAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return new Response<UserVm>("El correo electrónico es requerido", false);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            // Consulta de UN usuario por su correo: read_user_accounts solo filtra por coincidencia
            // parcial (email_like), así que se traen varias coincidencias y se conserva únicamente la
            // exacta (UserEmailMatch). Antes, con page_size = 1, "ana@…" podía devolver a "juana@…".
            // La búsqueda por similitud sigue en GET User (email_like) y GET User/organization (Email).
            var payloadObj = new
            {
                page_number = 1,
                page_size = 50,
                filter = new { email_like = email.Trim(), is_active = true }
            };

            var payload = JsonConvert.SerializeObject(payloadObj, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_user_accounts",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result) || result == "{}")
                return new Response<UserVm>("Usuario no encontrado", false);

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<UserDto>>(result);
            var userDto = UserEmailMatch.PickExact(wrapper?.Data, email);

            if (userDto == null)
                return new Response<UserVm>("Usuario no encontrado", false);

            var userVm = _mapper.Map<UserVm>(userDto);

            if (userVm.PersonnelId != Guid.Empty)
            {
                var personnelResult = await _personApiService.GetPersonByIdAsync(userVm.PersonnelId);
                userVm.Personnel = personnelResult.Succeeded ? personnelResult.Data : null;
            }

            // Obtener todos los organization IDs en los que está el usuario
            try
            {
                var orgResult = await _connection.QueryFirstOrDefaultAsync<string>(
                    "read_user_organizations",
                    new { userId = userVm.UserId },
                    commandType: CommandType.StoredProcedure);

                if (!string.IsNullOrEmpty(orgResult) && orgResult != "{}")
                {
                    var userOrgDtos = JsonConvert.DeserializeObject<List<UserOrganizationDto>>(orgResult);
                    if (userOrgDtos != null && userOrgDtos.Any())
                    {
                        userVm.OrganizationIds = [.. userOrgDtos
                            .Select(uo => uo.OrganizationId)
                            .Distinct()];

                    }
                }
            }
            catch
            {
                // Si falla la consulta de organizaciones, continuar sin ellas
            }

            return new Response<UserVm>(userVm, "OK");
        }
        catch (Exception ex)
        {
            return new Response<UserVm>($"Error al obtener el usuario: {ex.Message}", false);
        }
    }

    public async Task<Response<UserVm>> GetByIdInfoAsync(Guid UserId, int? organizationId = null)
    {
        try
        {
            if (UserId == Guid.Empty)
                return new Response<UserVm>("El ID de usuario es requerido", false);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            var payloadObj = new
            {
                page_number = 1,
                page_size = 1,
                filter = new { user_id = UserId, organization_id = organizationId, is_active = true }
            };

            var payload = JsonConvert.SerializeObject(payloadObj, settings);

            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_user_accounts",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result) || result == "{}")
                return new Response<UserVm>("Usuario no encontrado", false);

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<UserDto>>(result);
            var userDto = wrapper?.Data?.FirstOrDefault();

            if (userDto == null)
                return new Response<UserVm>("Usuario no encontrado", false);

            var userVm = _mapper.Map<UserVm>(userDto);

            if (userVm.PersonnelId != Guid.Empty)
            {
                var personnelResult = await _personApiService.GetPersonByIdAsync(userVm.PersonnelId);
                userVm.Personnel = personnelResult.Succeeded ? personnelResult.Data : null;
            }

            // Obtener todos los organization IDs en los que está el usuario
            try
            {
                var orgResult = await _connection.QueryFirstOrDefaultAsync<string>(
                    "read_user_organizations",
                    new { userId = userVm.UserId },
                    commandType: CommandType.StoredProcedure);

                if (!string.IsNullOrEmpty(orgResult) && orgResult != "{}")
                {
                    var userOrgDtos = JsonConvert.DeserializeObject<List<UserOrganizationDto>>(orgResult);
                    if (userOrgDtos != null && userOrgDtos.Any())
                    {
                        userVm.OrganizationIds = [.. userOrgDtos
                            .Select(uo => uo.OrganizationId)
                            .Distinct()];

                    }
                }
            }
            catch
            {
                // Si falla la consulta de organizaciones, continuar sin ellas
            }

            return new Response<UserVm>(userVm, "OK");
        }
        catch (Exception ex)
        {
            return new Response<UserVm>($"Error al obtener el usuario: {ex.Message}", false);
        }
    }
}

