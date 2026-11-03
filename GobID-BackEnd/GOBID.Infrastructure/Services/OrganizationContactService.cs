using AutoMapper;
using Dapper;
using GOBID.Application.DTOs.OrganizationContact;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Claim;
using GOBID.Application.ViewModels.OrganizationContact;
using GOBID.Application.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;

namespace GOBID.Infrastructure.Services;

public class OrganizationContactService: IOrganizationContactService
{
    private readonly IDbConnection _connection;
    private readonly IMapper _mapper;
    public OrganizationContactService(IDbConnection connection, IMapper mapper)
    {
        _connection = connection;
        _mapper = mapper;
    }

    public async Task<Response<string>> UpsertAsync(OrganizationContactUpsertDto[] dtos)
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
                    "upsert_organization_contact",
                    new { payload },
                    commandType: CommandType.StoredProcedure);

                if (string.IsNullOrEmpty(result))
                    return new Response<string>("No se pudo insertar/actualizar uno o más contactos", false);
            }

            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }

    }

    public async Task<Response<string>> DeleteAsync(int contactId)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "delete_organization_contact",
                new { contact_id = contactId },
                commandType: CommandType.StoredProcedure);
            if (string.IsNullOrEmpty(result))
                return new Response<string>("No se pudo eliminar el contacto", false);
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<List<OrganizationContactVm>>> ListAsync(int organizationId)
    {
        try
        {
            var result = await _connection.QueryFirstOrDefaultAsync<string>(
                "read_organization_contacts",
                new { organization_id = organizationId },
                commandType: CommandType.StoredProcedure
            );

            if (string.IsNullOrWhiteSpace(result) || result == "[]" || result == "{}")
                return new Response<List<OrganizationContactVm>>("Sin resultados", false);

            var dtos = JsonConvert.DeserializeObject<List<OrganizationContactDto>>(result);

            if (dtos == null || dtos.Count == 0)
                return new Response<List<OrganizationContactVm>>("Sin resultados", false);

            var vms = _mapper.Map<List<OrganizationContactVm>>(dtos);

            return new Response<List<OrganizationContactVm>>(vms, "Ok");
        }
        catch (Exception ex)
        {
            return new Response<List<OrganizationContactVm>>($"Error: {ex.Message}", false);
        }
    }
}
