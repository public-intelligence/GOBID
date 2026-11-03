using AutoMapper;
using Dapper;
using GOBID.Application.DTOs;
using GOBID.Application.DTOs.Organization;
using GOBID.Application.DTOs.OrganizationApplication;
using GOBID.Application.DTOs.Roles;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Organization;
using GOBID.Application.ViewModels.OrganizationApplication;
using GOBID.Application.ViewModels.OrganizationContact;
using GOBID.Application.ViewModels.Roles;
using GOBID.Application.Wrappers;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;

namespace GOBID.Infrastructure.Services;

public class OrganizationService : IOrganizationService
{
    private readonly IDbConnection _connection;
    private readonly IMapper _mapper;
    private readonly IOrganizationContactService _organizationContactService;
    private readonly ILegalEntityService _legalEntityService;

    public OrganizationService(IDbConnection connection, IMapper mapper, IOrganizationContactService organizationContactService, ILegalEntityService legalEntityService)
    {
        _connection = connection;
        _mapper = mapper;
        _organizationContactService = organizationContactService;
        _legalEntityService = legalEntityService;
    }

    private async Task EnrichOrganizationWithLegalEntityAsync(OrganizationVm org)
    {
        if (org.LegalEntityId != Guid.Empty)
        {
            try
            {
                var legalEntityResponse = await _legalEntityService.GetOrganizationByIdAsync(org.LegalEntityId);
                if (legalEntityResponse.Succeeded && legalEntityResponse.Data != null)
                {
                    org.LegalEntity = legalEntityResponse.Data;
                }
            }
            catch
            {
                // Ignorar errores al obtener legal entity para no romper el flujo principal
            }
        }
    }

    public async Task<Response<OrganizationVm>> UpsertAsync(OrganizationUpsertDto dto)
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
                "upsert_organization",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<OrganizationVm>("No se pudo insertar/actualizar la organizacion", false);

            var orgDto = JsonConvert.DeserializeObject<OrganizationDto>(result);
            if (orgDto == null)
                return new Response<OrganizationVm>("No se pudo deserializar el rol", false);

            var vm = _mapper.Map<OrganizationVm>(orgDto);
            return new Response<OrganizationVm>(vm, "OK");
        }
        catch (SqlException ex) when (ex.Number == 51000)
        {
            try
            {
                var err = JsonConvert.DeserializeObject<dynamic>(ex.Message);
                var msg = (string)(err?.message ?? ex.Message);
                return new Response<OrganizationVm>(msg, false);
            }
            catch
            {
                return new Response<OrganizationVm>($"Error: {ex.Message}", false);
            }
        }
        catch (Exception ex)
        {
            return new Response<OrganizationVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DeleteAsync(int orgId, Guid deletedBy)
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
            var payload = JsonConvert.SerializeObject(new { organization_id = orgId, deleted_by = deletedBy }, settings);
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "delete_organization",
                new { payload },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo eliminar la organización", false);
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<PagedResponse<OrganizationVm>> GetOrganizationsAsync(int? pageNumber, int? pageSize, OrganizationFilterDto? filter, int? organizationId)
    {
        if (pageNumber == null || pageNumber <= 0)
            pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100)
            pageSize = 50;

        try
        {
            if (organizationId != null)
            {
                var orgResp = await GetAsync(1, 1, organizationId.Value);
                if (!orgResp.Succeeded || orgResp.Data == null)
                    return new PagedResponse<OrganizationVm>([], "Sin resultados");

                var vms = new List<OrganizationVm> { orgResp.Data };

                var appMapsResult = await _connection.QueryFirstOrDefaultAsync<string>(
                    "read_org_app_maps",
                    new { page_number = 1, page_size = 10000 },
                    commandType: CommandType.StoredProcedure);

                if (!string.IsNullOrEmpty(appMapsResult) && appMapsResult != "{}")
                {
                    var appMapsWrapper = JsonConvert.DeserializeObject<OrganizationapplicationResultWrapper>(appMapsResult);
                    if (appMapsWrapper?.Data != null)
                    {
                        var org = vms[0];
                        org.Count = appMapsWrapper.Data.Count(x => x.OrganizationId == org.OrganizationId && x.IsActive);
                    }
                }

                return new PagedResponse<OrganizationVm>(
                    vms,
                    pageNumber ?? 1,
                    pageSize ?? 50,
                    totalCount: 1,
                    totalPages: 1
                );
            }

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
                "read_organizations",
                new { page_number = pageNumber, page_size = pageSize, filter = filterJson },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result) || result == "{}")
                return new PagedResponse<OrganizationVm>([], "Sin resultados");

            var wrapper = JsonConvert.DeserializeObject<ResultWrapper<OrganizationDto>>(result);
            var dtos = wrapper?.Data ?? new List<OrganizationDto>();
            var vmsList = _mapper.Map<List<OrganizationVm>>(dtos);

            var appMapsResultAll = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_org_app_maps",
                new { page_number = 1, page_size = 10000 },
                commandType: CommandType.StoredProcedure);

            if (!string.IsNullOrEmpty(appMapsResultAll) && appMapsResultAll != "{}")
            {
                var appMapsWrapper = JsonConvert.DeserializeObject<OrganizationapplicationResultWrapper>(appMapsResultAll);
                if (appMapsWrapper?.Data != null)
                {
                    foreach (var org in vmsList)
                    {
                        org.Count = appMapsWrapper.Data
                            .Count(x => x.OrganizationId == org.OrganizationId && x.IsActive);
                    }
                }
            }

            // Enriquecer cada organización con su LegalEntity
            foreach (var org in vmsList)
            {
                await EnrichOrganizationWithLegalEntityAsync(org);
            }

            return new PagedResponse<OrganizationVm>(
                vmsList,
                wrapper?.CurrentPage ?? (pageNumber ?? 1),
                pageSize.Value,
                wrapper?.TotalTally ?? vmsList.Count,
                wrapper?.TotalPages ?? 1
            );
        }
        catch (Exception ex)
        {
            return new PagedResponse<OrganizationVm>(new List<OrganizationVm>(),
                "Error al listar las organizaciones",
                new List<string> { ex.Message }
            );
        }
    }

    public async Task<Response<OrganizationVm>> GetAsync(int pageNumber, int pageSize, int orgId)
    {
        try
        {
            var filterJson = JsonConvert.SerializeObject(new { orgid = orgId });
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_organizations",
                new { page_number = pageNumber, page_size = pageSize, filter = filterJson },
                commandType: CommandType.StoredProcedure);

            if (string.IsNullOrEmpty(result) || result == "{}")
                return new Response<OrganizationVm>("Sin resultados", false);

            var wrapper = JsonConvert.DeserializeObject<OrganizationResultWrapperDto>(result);
            if (wrapper?.Data == null || !wrapper.Data.Any())
                return new Response<OrganizationVm>("Sin datos", false);


            var vms = _mapper.Map<OrganizationVm>(wrapper.Data.First());

            try
            {
                var contactsResponse = await _organizationContactService.ListAsync(vms.OrganizationId);
                if (contactsResponse.Succeeded && contactsResponse.Data != null)
                {
                    vms.Contact = contactsResponse.Data.Any()
                        ? contactsResponse.Data.ToArray()
                        : Array.Empty<OrganizationContactVm>();
                }
            }
            catch
            {
                // Ignorar errores del servicio de contactos para no romper el flujo principal
            }

            // Enriquecer con LegalEntity
            await EnrichOrganizationWithLegalEntityAsync(vms);

            return new Response<OrganizationVm>(vms, "OK");
        }
        catch (Exception ex)
        {
            return new Response<OrganizationVm>($"Error: {ex.Message}", false);
        }
    }
}