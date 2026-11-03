using AutoMapper;
using IdentityHub.Application.Repositories;
using IdentityHub.Domain.DTOs.Organizations;
using IdentityHub.Domain.DTOs.Pagination;
using IdentityHub.Domain.DTOs.Persons;
using IdentityHub.Domain.Enums;
using IdentityHub.Domain.Models;
using IdentityHub.Domain.Response;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace IdentityHub.Service.Services;

public class LegalEntityServices : IOrganizationServices
{
    private readonly IRepository<LegalEntity> _organizationRepository;
    private readonly IMapper _mapper;
    private readonly IPaginationService _paginationService;


    public LegalEntityServices(IRepository<LegalEntity> organizationRepository, IMapper mapper, IPaginationService paginationService)
    {
        _organizationRepository = organizationRepository;
        _mapper = mapper;
        _paginationService = paginationService;
    }


    public async Task<IHttpResult<LegalEntityDTO>> CreateAsync(CreateLegalEntityDTO createOrganizationDTO)
    {
        var details = new Dictionary<string, IEnumerable<string>>();
        var organizationExistsResult = await _organizationRepository.GetAsync(
            o => o.LegalName.ToLower().Trim() == createOrganizationDTO.LegalName.ToLower().Trim()
        );
        if (!organizationExistsResult.IsSuccess)
        {
            details.TryAdd("errors", ["No se pudo verificar si el nombre legal ya esta registrado"]);
        }

        var organizationExists = organizationExistsResult.Value;
        if (organizationExists != null)
        {
            details.TryAdd("legalName", ["Ya existe una organización con el mismo nombre legal"]);
        }
        var orgSocialNameExistResult = await _organizationRepository.GetAsync(
           o => o.SocialName.ToLower().Trim() == createOrganizationDTO.SocialName.ToLower().Trim()
       );
        if (!orgSocialNameExistResult.IsSuccess)
        {
            details.TryAdd("errors", ["No se pudo verificar si el nombre social ya esta registrado"]);
        }

        var orgSocialNameExist = orgSocialNameExistResult.Value;
        if (orgSocialNameExist != null)
        {
            details.TryAdd("socialName", ["Ya existe una organización con el mismo nombre social"]);
        }


        var rncExistResult = await _organizationRepository.GetAsync(
            o => o.IdentificationNumber == createOrganizationDTO.IdentificationNumber
        );
        if (!rncExistResult.IsSuccess)
        {
            details.TryAdd("errors", ["No se pudo verificar si el RNC ya esta registrado"]);
        }

        var rncExist = rncExistResult.Value;
        if (rncExist != null)
        {
            details.TryAdd("rnc", ["Ya existe una organización con el mismo RNC"]);
        }

        if (details.Count > 0)
        {
            var title = "No se pudo crear la organización";
            return HttpResult<LegalEntityDTO>.Failure(title, details, HttpStatusCode.BadRequest);
        }

        var organization = _mapper.Map<LegalEntity>(createOrganizationDTO);

        var createResult = await _organizationRepository.CreateAsync(organization);
        if (!createResult.IsSuccess)
        {
            var title = "No se pudo crear la organización";
            details.TryAdd("errors", ["Ocurrió un error al crear la organización"]);
            return HttpResult<LegalEntityDTO>.Failure(title, details, HttpStatusCode.InternalServerError);
        }

        var resultOrganization = createResult.Value;
        var organizationDTO = _mapper.Map<LegalEntityDTO>(resultOrganization);
        return HttpResult<LegalEntityDTO>.Success(organizationDTO, HttpStatusCode.Created);
    }

    public async Task<IHttpResult<LegalEntityDTO>> GetByIdAsync(Guid id)
    {
        var details = new Dictionary<string, IEnumerable<string>>();
        var organizationResult = await _organizationRepository.GetAsync(o => o.Id == id);
        if (!organizationResult.IsSuccess)
        {
            var title = "No se pudo obtener la organización";
            details.TryAdd("errors", ["Ocurrió un error al obtener la organización"]);
            return HttpResult<LegalEntityDTO>.Failure(title, details, HttpStatusCode.InternalServerError);
        }
        var organization = organizationResult.Value;
        if (organization == null)
        {
            var title = "Organización no encontrada";
            details.TryAdd("id", ["No se encontró una organización con el ID proporcionado"]);
            return HttpResult<LegalEntityDTO>.Failure(title, details, HttpStatusCode.NotFound);
        }
        var organizationDTO = _mapper.Map<LegalEntityDTO>(organization);
        return HttpResult<LegalEntityDTO>.Success(organizationDTO, HttpStatusCode.OK);
    }

    
    public async Task<IHttpResult<List<LegalEntityDTO>>> GetAllAsync()
    {
        var details = new Dictionary<string, IEnumerable<string>>();
        var organizationsResult = await _organizationRepository.GetAllAsync();
        if (!organizationsResult.IsSuccess)
        {
            var title = "No se pudieron obtener las organizaciones";
            details.TryAdd("errors", ["Ocurrió un error al obtener las organizaciones"]);
            return HttpResult<List<LegalEntityDTO>>.Failure(title, details, HttpStatusCode.InternalServerError);
        }
        var organizations = organizationsResult.Value;
        var organizationDTOs = _mapper.Map<List<LegalEntityDTO>>(organizations);
        return HttpResult<List<LegalEntityDTO>>.Success(organizationDTOs, HttpStatusCode.OK);
    }

    public async Task<IHttpResult<PagedResponseDTO<LegalEntityDTO>>> GetAllAsync(PaginationFilterDTO paginationFilter, HttpContext httpContext, string? searchTerm = null, EnumStatus? status = null)
    {
        var pageNumber = paginationFilter.PageNumber < 1 ? 1 : paginationFilter.PageNumber;

        var pageSize = paginationFilter.PageSize;
        var organizationCountResult = await _organizationRepository.CountAsync(
            p => (string.IsNullOrWhiteSpace(searchTerm) ||
                  p.LegalName.Contains(searchTerm) ||
                  p.SocialName.Contains(searchTerm) ||
                  p.IdentificationNumber.Replace("-", "").Contains(searchTerm.Replace("-", "")) ||
                  p.Email.Contains(searchTerm)) &&
                 (!status.HasValue || p.Status == status.Value)
        );
        var organizationCount = organizationCountResult.Value;

        var result = await _organizationRepository.FilterAsync(
            p => (string.IsNullOrWhiteSpace(searchTerm) ||
                  p.LegalName.Contains(searchTerm) ||
                  p.SocialName.Contains(searchTerm) ||
                  p.IdentificationNumber.Replace("-", "").Contains(searchTerm.Replace("-", "")) ||
                  p.Email.Contains(searchTerm)) &&
                 (!status.HasValue || p.Status == status.Value),
            query =>
                query
                    .OrderBy(e => e.LegalName)
                    .ThenBy(e => e.SocialName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
        );

        if (!result.IsSuccess)
        {
            var title = "Error al obtener los registros";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudieron obtener los registros."] } };
            return HttpResult<PagedResponseDTO<LegalEntityDTO>>.Failure(
                title, detail, HttpStatusCode.BadRequest
            );
        }

        var organizations = result.Value;
        var organizationDtos = _mapper.Map<IEnumerable<LegalEntityDTO>>(organizations);

        var pagedResponse = _paginationService.BuildPaginationUrl(httpContext, pageNumber, pageSize, organizationCount, organizationDtos);

        return HttpResult<PagedResponseDTO<LegalEntityDTO>>.Success(pagedResponse);
    }

    public async Task<IHttpResult<LegalEntityDTO>> UpdateAsync(Guid id, UpdateOrganizationDTO updateOrganizationDTO)
    {
        var details = new Dictionary<string, IEnumerable<string>>();
        var organizationResult = await _organizationRepository.GetAsync(o => o.Id == id);
        if (!organizationResult.IsSuccess)
        {
            var title = "No se pudo obtener la organización";
            details.TryAdd("errors", ["Ocurrió un error al obtener la organización"]);
            return HttpResult<LegalEntityDTO>.Failure(title, details, HttpStatusCode.InternalServerError);
        }
        var organization = organizationResult.Value;
        if (organization == null)
        {
            var title = "Organización no encontrada";
            details.TryAdd("id", ["No se encontró una organización con el ID proporcionado"]);
            return HttpResult<LegalEntityDTO>.Failure(title, details, HttpStatusCode.NotFound);
        }

        if (!string.IsNullOrWhiteSpace(updateOrganizationDTO.LegalName))
        {
            var legalNameExistsResult = await _organizationRepository.GetAsync(
                o => o.LegalName.ToLower().Trim() == updateOrganizationDTO.LegalName.ToLower().Trim() && o.Id != id
            );
            if (!legalNameExistsResult.IsSuccess)
            {
                details.TryAdd("errors", ["No se pudo verificar si el nombre legal ya esta registrado"]);
            }
            else if (legalNameExistsResult.Value != null)
            {
                details.TryAdd("legalName", ["Ya existe una organización con el mismo nombre legal"]);
            }
        }

        if (!string.IsNullOrWhiteSpace(updateOrganizationDTO.SocialName))
        {
            var socialNameExistsResult = await _organizationRepository.GetAsync(
                o => o.SocialName.ToLower().Trim() == updateOrganizationDTO.SocialName.ToLower().Trim() && o.Id != id
            );
            if (!socialNameExistsResult.IsSuccess)
            {
                details.TryAdd("errors", ["No se pudo verificar si el nombre social ya esta registrado"]);
            }
            else if (socialNameExistsResult.Value != null)
            {
                details.TryAdd("socialName", ["Ya existe una organización con el mismo nombre social"]);
            }
        }

        if (!string.IsNullOrWhiteSpace(updateOrganizationDTO.IdentificationNumber))
        {
            var rncExistsResult = await _organizationRepository.GetAsync(
                o => o.IdentificationNumber == updateOrganizationDTO.IdentificationNumber && o.Id != id
            );
            if (!rncExistsResult.IsSuccess)
            {
                details.TryAdd("errors", ["No se pudo verificar si el RNC ya esta registrado"]);
            }
            else if (rncExistsResult.Value != null)
            {
                details.TryAdd("rnc", ["Ya existe una organización con el mismo RNC"]);
            }
        }

        if (!string.IsNullOrWhiteSpace(updateOrganizationDTO.Email))
        {
            var emailExistsResult = await _organizationRepository.GetAsync(
                o => o.Email.ToLower().Trim() == updateOrganizationDTO.Email.ToLower().Trim() && o.Id != id
            );
            if (!emailExistsResult.IsSuccess)
            {
                details.TryAdd("errors", ["No se pudo verificar si el email ya esta registrado"]);
            }
            else if (emailExistsResult.Value != null)
            {
                details.TryAdd("email", ["Ya existe una organización con el mismo email"]);
            }
        }

        if (details.Count > 0)
        {
            var title = "No se pudo actualizar la organización";
            return HttpResult<LegalEntityDTO>.Failure(title, details, HttpStatusCode.BadRequest);
        }

        _mapper.Map(updateOrganizationDTO, organization);
        var updateResult = await _organizationRepository.PartialUpdateAsync(o => o.Id == id, organization);
        if (!updateResult.IsSuccess)
        {                                                       
            var title = "No se pudo actualizar la organización";
            details.TryAdd("errors", ["Ocurrió un error al actualizar la organización"]);
            return HttpResult<LegalEntityDTO>.Failure(title, details, HttpStatusCode.InternalServerError);
        }
        var updatedOrganization = updateResult.Value;
        var organizationDTO = _mapper.Map<LegalEntityDTO>(updatedOrganization);
        return HttpResult<LegalEntityDTO>.Success(organizationDTO, HttpStatusCode.OK);
    }

    public async Task<IHttpResult<LegalEntityDTO>> GetByIdentificationNumberAsync(string identificationNumber)
    {
        var details = new Dictionary<string, IEnumerable<string>>();
        var organizationResult = await _organizationRepository.GetAsync(o => o.IdentificationNumber == identificationNumber);
        if (!organizationResult.IsSuccess)
        {
            var title = "No se pudo obtener la organización";
            details.TryAdd("errors", ["Ocurrió un error al obtener la organización"]);
            return HttpResult<LegalEntityDTO>.Failure(title, details, HttpStatusCode.BadRequest);
        }
        var organization = organizationResult.Value;
        if (organization == null)
        {
            var title = "Organización no encontrada";
            details.TryAdd("id", ["No se encontró una organización con el RNC o Cedula proporcionado"]);
            return HttpResult<LegalEntityDTO>.Failure(title, details, HttpStatusCode.NotFound);
        }
        var organizationDTO = _mapper.Map<LegalEntityDTO>(organization);
        return HttpResult<LegalEntityDTO>.Success(organizationDTO, HttpStatusCode.OK);
    }
}
