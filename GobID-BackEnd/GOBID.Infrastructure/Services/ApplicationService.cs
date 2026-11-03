using AutoMapper;
using Dapper;
using GOBID.Application.DTOs;
using GOBID.Application.DTOs.Application;
using GOBID.Application.DTOs.Claim;
using GOBID.Application.DTOs.OrganizationApplication;
using GOBID.Application.DTOs.Roleclaim;
using GOBID.Application.DTOs.Roles;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Application;
using GOBID.Application.ViewModels.ApplicationAccess;
using GOBID.Application.ViewModels.Claim;
using GOBID.Application.ViewModels.OrganizationApplication;
using GOBID.Application.ViewModels.RoleClaim;
using GOBID.Application.ViewModels.Roles;
using GOBID.Application.Wrappers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;

namespace GOBID.Infrastructure.Services;

public class ApplicationService : IApplicationService
{
    private readonly IDbConnection _connection;
    private readonly IMapper _mapper;
    private readonly IClaimService _claimService;
    private readonly IRoleService _roleService;
    private readonly IRoleClaimService _roleClaimService;
    private readonly IServiceProvider _serviceProvider;
    public ApplicationService(
        IDbConnection connection,
        IMapper mapper,
        IClaimService claimService,
        IRoleService roleService,
        IRoleClaimService roleClaimService,
        IServiceProvider serviceProvider)
    {
        _connection = connection;
        _mapper = mapper;
        _claimService = claimService;
        _roleService = roleService;
        _roleClaimService = roleClaimService;
        // IOrganizationApplicationService se resuelve de forma perezosa desde _serviceProvider
        // para evitar un ciclo de DI (OrganizationApplicationService depende de IApplicationService).
        _serviceProvider = serviceProvider;
    }
    public async Task<Response<string>> UpsertAsync(ApplicationUpsertDto dto)
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
                "upsert_application",
                new { payload },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo insertar/actualizar la aplicaci\u00f3n", false);

            Guid applicationId;
            if (dto.ApplicationId.HasValue && dto.ApplicationId.Value != Guid.Empty)
            {
                applicationId = dto.ApplicationId.Value;
            }
            else
            {
                try
                {
                    var resultObj = JsonConvert.DeserializeObject<dynamic>(result);
                    var appIdRaw = (resultObj?.application_id ?? resultObj?.ApplicationId ?? resultObj?.applicationId ?? resultObj?.id) as object;

                    if (appIdRaw == null)
                        return new Response<string>("No se pudo obtener el identificador de la aplicaci\u00f3n", false);

                    string? appIdStr = appIdRaw.ToString();
                    if (!Guid.TryParse(appIdStr, out applicationId) || applicationId == Guid.Empty)
                        return new Response<string>("No se pudo obtener el identificador de la aplicaci\u00f3n", false);

                    dto.ApplicationId = applicationId;
                }
                catch
                {
                    return new Response<string>("No se pudo obtener el identificador de la aplicaci\u00f3n", false);
                }
            }

            if (dto.Claims == null || dto.Claims.Length == 0)
                return new Response<string>("Operacion exitosa", true);

            var existingClaims = await GetAllClaimsAsync(applicationId);

            var existingByCode = existingClaims.ToDictionary(c => c.Code!, StringComparer.OrdinalIgnoreCase);

            var updater = dto.UpdatedBy ?? dto.CreatedBy ?? Guid.Empty;
            foreach (var claim in dto.Claims)
            {
                if (!claim.ApplicationId.HasValue || claim.ApplicationId.Value == Guid.Empty)
                    claim.ApplicationId = applicationId;

                claim.UpdatedBy ??= updater;

                if (existingByCode.TryGetValue(claim.Code, out var existing))
                {
                    claim.ClaimId = existing.ClaimId;
                }
            }

            var upsertClaimsResp = await _claimService.UpsertAsync(dto.Claims);
            if (!upsertClaimsResp.Succeeded)
                return upsertClaimsResp;

            var incomingCodes = new HashSet<string>(dto.Claims.Select(c => c.Code!), StringComparer.OrdinalIgnoreCase);
            var toDelete = existingClaims.Where(c => !incomingCodes.Contains(c.Code!)).ToList();

            foreach (var c in toDelete)
            {
                var delResp = await _claimService.DeleteAsync(c.ClaimId, updater);
                if (!delResp.Succeeded)
                    return delResp;
            }

            return new Response<string>("Operacion exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al insertar/actualizar la aplicaci\u00f3n: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DeleteAsync(Guid appId, ApplicationDeleteDto dto)
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
                application_id = appId,
                row_version = dto.RowVersion,
                updated_by = dto.UpdatedBy
            }, settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "delete_application",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo eliminar la aplicaci\u00f3n", false);
            return new Response<string>("Operaci\u00f3n exitosa", true);
        }
        catch
        {
            return new Response<string>("Error al eliminar la aplicaci\u00f3n", false);
        }
    }

    public async Task<PagedResponse<ApplicationVm>> ListAsync(
        int? pageNumber,
        int? pageSize,
        ApplicationFilterDto? filter)
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
                "read_applications",
                new { page_number = pageNumber, page_size = pageSize, filter = filterJson },
                commandType: CommandType.StoredProcedure);

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<ApplicationDto>>(result ?? "{}");

            var dtos = wrapper?.Data ?? new List<ApplicationDto>();
            var vms = _mapper.Map<List<ApplicationVm>>(dtos);

            return new PagedResponse<ApplicationVm>(
                vms,
                wrapper?.CurrentPage ?? 1,
                pageSize.Value,
                wrapper?.TotalTally ?? 0,
                wrapper?.TotalPages ?? 1
            );
        }
        catch (Exception ex)
        {
            return new PagedResponse<ApplicationVm>(
                new List<ApplicationVm>(),
                "Error al listar las aplicaciones", new List<string> { ex.Message }
            );
        }
    }

    public async Task<Response<ApplicationVm>> GetAsync(int? pageNumber, int? pageSize, Guid appId)
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

            var filterObj = new { application_id = appId };
            var filterJson = JsonConvert.SerializeObject(filterObj, settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_applications",
                new { filter = filterJson },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<ApplicationVm>("Sin resultados", false);

            var json = JsonConvert.DeserializeObject<ResultWrapper<ApplicationDto>>(result);
            var application = json?.Data?.FirstOrDefault();

            if (application == null)
                return new Response<ApplicationVm>("Sin resultados", false);

            var vm = _mapper.Map<ApplicationVm>(application);
            return new Response<ApplicationVm>(vm, "OK");
        }
        catch
        {
            return new Response<ApplicationVm>("Error al obtener la aplicaci\u00f3n", false);
        }
    }

    public async Task<Response<string>> GetGeneratestring(Guid appId)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "upsert_generate_string",
                new { application_id = appId },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo listar la cadena de la aplicaci\u00f3n", false);
            return new Response<string>(result, "OK");
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al listar la cadena de la aplicaci\u00f3n: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DuplicateAsync(Guid appId, Guid userId)
    {
        try
        {
            var sourceResp = await GetAsync(1, 1, appId);
            if (!sourceResp.Succeeded || sourceResp.Data == null)
                return new Response<string>("No se pudo obtener la aplicaci\u00f3n a duplicar", false);

            var source = sourceResp.Data;
            var sourceClaims = await GetAllClaimsAsync(appId);

            // Crea la aplicaci\u00f3n duplicada junto con sus claims (reutiliza UpsertAsync,
            // que inserta los claims recibidos porque la nueva aplicaci\u00f3n a\u00fan no tiene ninguno).
            var newAppDto = new ApplicationUpsertDto
            {
                ApplicationName = $"{source.ApplicationName} copy",
                Code = $"{source.Code} copy",
                RedirectUrl = source.RedirectUrl ?? string.Empty,
                ApplicationDescription = source.ApplicationDescription ?? string.Empty,
                IsActive = source.IsActive,
                IsPublic = source.IsPublic,
                CreatedBy = userId,
                UpdatedBy = userId,
                Claims = sourceClaims.Select(c => new ClaimUpsertDto
                {
                    Code = c.Code!,
                    ClaimName = c.ClaimName!,
                    ClaimDescription = c.ClaimDescription,
                    IsAutomatic = c.IsAutomatic,
                    IsActive = c.IsActive,
                    UpdatedBy = userId
                }).ToArray()
            };

            var upsertResp = await UpsertAsync(newAppDto);
            if (!upsertResp.Succeeded || newAppDto.ApplicationId is not Guid newAppId || newAppId == Guid.Empty)
                return new Response<string>("No se pudo crear la aplicaci\u00f3n duplicada", false);

            // Mapea claim antiguo -> claim nuevo (por Code) para poder duplicar los RoleClaims m\u00e1s abajo.
            var claimIdMap = new Dictionary<int, int>();
            if (sourceClaims.Count > 0)
            {
                var newClaims = await GetAllClaimsAsync(newAppId);
                var newClaimsByCode = newClaims
                    .Where(c => c.Code != null)
                    .GroupBy(c => c.Code!, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().ClaimId, StringComparer.OrdinalIgnoreCase);

                foreach (var oldClaim in sourceClaims)
                {
                    if (oldClaim.Code != null && newClaimsByCode.TryGetValue(oldClaim.Code, out var newClaimId))
                        claimIdMap[oldClaim.ClaimId] = newClaimId;
                }
            }

            // IOrganizationApplicationService se resuelve en tiempo de ejecuci\u00f3n (ver constructor).
            var organizationApplicationService = _serviceProvider.GetRequiredService<IOrganizationApplicationService>();

            var sourceOrgApps = await GetAllOrgApplicationsAsync(organizationApplicationService, appId);
            foreach (var sourceOrgApp in sourceOrgApps)
            {
                var orgAppUpsertResp = await organizationApplicationService.UpsertAsync(new OrganizationApplicationUpsertDto
                {
                    OrganizationId = sourceOrgApp.OrganizationId,
                    ApplicationId = newAppId,
                    IsActive = sourceOrgApp.IsActive
                });
                if (!orgAppUpsertResp.Succeeded)
                    return new Response<string>("No se pudo vincular la aplicaci\u00f3n duplicada a las organizaciones", false);
            }

            var newOrgApps = await GetAllOrgApplicationsAsync(organizationApplicationService, newAppId);

            foreach (var sourceOrgApp in sourceOrgApps)
            {
                var newOrgApp = newOrgApps.FirstOrDefault(o => o.OrganizationId == sourceOrgApp.OrganizationId);
                if (newOrgApp == null)
                    continue;

                var sourceRoles = await GetAllRolesAsync(sourceOrgApp.OrganizationApplicationId);
                foreach (var sourceRole in sourceRoles)
                {
                    var roleUpsertResp = await _roleService.UpsertAsync(new RoleUpsertDto
                    {
                        RoleName = sourceRole.RoleName,
                        RoleDescription = sourceRole.RoleDescription,
                        SetAutomaticClaims = sourceRole.SetAutomaticClaims,
                        OrganizationApplicationId = newOrgApp.OrganizationApplicationId,
                        PositionId = sourceRole.PositionId,
                        IsActive = sourceRole.IsActive,
                        CreatedBy = userId,
                        UpdatedBy = userId,
                        IsSystemRole = sourceRole.IsSystemRole
                    });
                    if (!roleUpsertResp.Succeeded || roleUpsertResp.Data == null)
                        return new Response<string>("No se pudo duplicar los roles de la aplicaci\u00f3n", false);

                    var newRoleId = roleUpsertResp.Data.RoleId;

                    var sourceRoleClaims = await GetAllRoleClaimsAsync(sourceRole.RoleId);
                    var newRoleClaims = sourceRoleClaims
                        .Where(rc => claimIdMap.ContainsKey(rc.ClaimId))
                        .Select(rc => new RoleClaimUpsertDto
                        {
                            RoleId = newRoleId,
                            ClaimId = claimIdMap[rc.ClaimId],
                            IsActive = rc.IsActive,
                            CreatedBy = userId,
                            UpdatedBy = userId
                        })
                        .ToArray();

                    if (newRoleClaims.Length > 0)
                    {
                        var roleClaimResp = await _roleClaimService.UpsertAsync(newRoleClaims);
                        if (!roleClaimResp.Succeeded)
                            return new Response<string>("No se pudo duplicar los permisos de los roles de la aplicaci\u00f3n", false);
                    }
                }
            }

            return new Response<string>(newAppId.ToString(), "Aplicaci\u00f3n duplicada correctamente");
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error al duplicar la aplicaci\u00f3n: {ex.Message}", false);
        }
    }

    private async Task<List<ClaimVm>> GetAllClaimsAsync(Guid applicationId)
    {
        var claims = new List<ClaimVm>();
        int page = 1;
        const int pageSize = 100;
        while (true)
        {
            var listResp = await _claimService.ListAsync(page, pageSize, new ClaimFilterDto { ApplicationId = applicationId });
            if (!listResp.Succeeded) break;
            var batch = listResp.Data ?? new List<ClaimVm>();
            if (batch.Count == 0) break;
            claims.AddRange(batch);
            if (batch.Count < pageSize) break;
            page++;
        }
        return claims;
    }

    private async Task<List<RoleVm>> GetAllRolesAsync(int organizationApplicationId)
    {
        var roles = new List<RoleVm>();
        int page = 1;
        const int pageSize = 100;
        while (true)
        {
            var listResp = await _roleService.ListAsync(page, pageSize, new RoleFilterDto { OrganizationApplicationId = organizationApplicationId });
            if (!listResp.Succeeded) break;
            var batch = listResp.Data ?? new List<RoleVm>();
            if (batch.Count == 0) break;
            roles.AddRange(batch);
            if (batch.Count < pageSize) break;
            page++;
        }
        return roles;
    }

    private async Task<List<RoleClaimVm>> GetAllRoleClaimsAsync(int roleId)
    {
        var roleClaims = new List<RoleClaimVm>();
        int page = 1;
        const int pageSize = 100;
        while (true)
        {
            var listResp = await _roleClaimService.ListAsync(page, pageSize, roleId, null);
            if (!listResp.Succeeded) break;
            var batch = listResp.Data ?? new List<RoleClaimVm>();
            if (batch.Count == 0) break;
            roleClaims.AddRange(batch);
            if (batch.Count < pageSize) break;
            page++;
        }
        return roleClaims;
    }

    private static async Task<List<OrganizationApplicationVm>> GetAllOrgApplicationsAsync(
        IOrganizationApplicationService organizationApplicationService, Guid applicationId)
    {
        var orgApps = new List<OrganizationApplicationVm>();
        int page = 1;
        const int pageSize = 100;
        while (true)
        {
            var listResp = await organizationApplicationService.ListAsync(
                page, pageSize, new OrganizationApplicationfilterDto { ApplicationId = applicationId });
            var batch = listResp.Data ?? new List<OrganizationApplicationVm>();
            if (batch.Count == 0) break;
            orgApps.AddRange(batch);
            if (batch.Count < pageSize) break;
            page++;
        }
        return orgApps;
    }
}
