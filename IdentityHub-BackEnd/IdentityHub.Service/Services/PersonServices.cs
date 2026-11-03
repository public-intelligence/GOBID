using AutoMapper;
using IdentityHub.Application.Repositories;
using IdentityHub.Domain.DTOs.Pagination;
using IdentityHub.Domain.DTOs.Persons;
using IdentityHub.Domain.Enums;
using IdentityHub.Domain.Models;
using IdentityHub.Domain.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Net;

namespace IdentityHub.Service.Services;

// Combina dos Expression<Func<T, bool>> con AND reescribiendo el parámetro del segundo
// para que apunte al del primero, en vez de anidar Invoke(): así el árbol resultante
// sigue siendo un simple AndAlso de comparaciones planas, que EF Core traduce a SQL sin problema.
internal static class ExpressionCombinationExtensions
{
    public static Expression<Func<T, bool>> AndAlsoExpr<T>(
        this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var rewrittenRight = new ParameterRebinder(right.Parameters[0], left.Parameters[0]).Visit(right.Body);
        var body = Expression.AndAlso(left.Body, rewrittenRight);
        return Expression.Lambda<Func<T, bool>>(body, left.Parameters[0]);
    }

    public static Expression<Func<T, bool>> OrElseExpr<T>(
        this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var rewrittenRight = new ParameterRebinder(right.Parameters[0], left.Parameters[0]).Visit(right.Body);
        var body = Expression.OrElse(left.Body, rewrittenRight);
        return Expression.Lambda<Func<T, bool>>(body, left.Parameters[0]);
    }

    private sealed class ParameterRebinder : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterRebinder(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }
}

public class PersonServices : IPersonServices
{
    private readonly IRepository<Person> _personRepository;
    private readonly IPaginationService _paginationService;
    private readonly IMapper _mapper;

    public PersonServices(IRepository<Person> personRepository, IMapper mapper, IPaginationService paginationService)
    {
        _personRepository = personRepository;
        _mapper = mapper;
        _paginationService = paginationService;
    }

    private static Expression<Func<Person, bool>> BuildNameSearchPredicate(string searchTerm, bool includeEmail = false)
    {
        var terms = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Expression<Func<Person, bool>> combined = p => true;
        foreach (var term in terms)
        {
            var capturedTerm = term;
            Expression<Func<Person, bool>> termMatches = p =>
        EF.Functions.Collate(p.FirstName, "Latin1_General_CI_AI").StartsWith(capturedTerm) ||
        EF.Functions.Collate(p.LastName, "Latin1_General_CI_AI").StartsWith(capturedTerm) ||
        EF.Functions.Collate(p.IdentificationNumber, "Latin1_General_CI_AI").StartsWith(capturedTerm) ||
        (includeEmail && p.Email != null && EF.Functions.Collate(p.Email, "Latin1_General_CI_AI").Contains(capturedTerm));

            combined = combined.AndAlsoExpr(termMatches);
        }

        return combined;
    }

    public async Task<IHttpResult<IEnumerable<PersonDTO>>> GetAllAsync()
    {
        var details = new Dictionary<string, IEnumerable<string>>();
        var personResult = await _personRepository.FilterAsync(p => true);
        if (!personResult.IsSuccess)
        {
            var title = "No se pudieron obtener las Personas";
            details.TryAdd("errors", ["Ocurrió un error al obtener las Personas"]);
            return HttpResult<IEnumerable<PersonDTO>>.Failure(title, details, HttpStatusCode.InternalServerError);
        }
        var persons = personResult.Value;
        var personDTOs = _mapper.Map<IEnumerable<PersonDTO>>(persons);
        return HttpResult<IEnumerable<PersonDTO>>.Success(personDTOs, HttpStatusCode.OK);
    }

    public async Task<IHttpResult<PagedResponseDTO<PersonDTO>>> GetAllAsync(PaginationFilterDTO paginationFilter, HttpContext httpContext, string? searchTerm = null, EnumStatus? status = null)
    {
        var pageNumber = paginationFilter.PageNumber < 1 ? 1 : paginationFilter.PageNumber;
        var pageSize = paginationFilter.PageSize;

        Expression<Func<Person, bool>> nameFilter = string.IsNullOrWhiteSpace(searchTerm)
            ? p => true
            : BuildNameSearchPredicate(searchTerm, includeEmail: false);

        Expression<Func<Person, bool>> statusFilter = p => !status.HasValue || p.Status == status.Value;

        var filter = nameFilter.AndAlsoExpr(statusFilter);

        var personnelCountResult = await _personRepository.CountAsync(filter);
        var personnelCount = personnelCountResult.Value;

        var personnelsResult = await _personRepository.FilterAsync(
            filter,
            query =>
                query
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
        );

        if (!personnelsResult.IsSuccess)
        {
            var title = "Error al obtener los registros";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudieron obtener los registros."] } };
            return HttpResult<PagedResponseDTO<PersonDTO>>.Failure(
                title, detail, HttpStatusCode.BadRequest
            );
        }

        var personnels = personnelsResult.Value;
        var personnelDtos = _mapper.Map<IEnumerable<PersonDTO>>(personnels);

        var pagedResponse = _paginationService.BuildPaginationUrl(httpContext, pageNumber, pageSize, personnelCount, personnelDtos);

        return HttpResult<PagedResponseDTO<PersonDTO>>.Success(pagedResponse);
    }

    public async Task<IHttpResult<PersonDTO>> GetAsyncById(Guid id)
    {
        var result = await _personRepository.GetAsync(p => p.Id == id);
        if (!result.IsSuccess)
        {
            var title = "Error al obtener a la Persona";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo encontrar a la Persona"] } };
            return HttpResult<PersonDTO>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        var person = result.Value;
        var personDto = _mapper.Map<PersonDTO>(person);

        return HttpResult<PersonDTO>.Success(personDto, HttpStatusCode.OK);

    }

    public async Task<IHttpResult<PersonDTO>> CreateAsync(CreatePersonDTO createPersonDto)
    {
        var details = new Dictionary<string, IEnumerable<string>>();

        var isExistingIdentificationNumberResult = await _personRepository.GetAsync(p => p.IdentificationNumber == createPersonDto.IdentificationNumber);
        if (isExistingIdentificationNumberResult.IsSuccess && isExistingIdentificationNumberResult.Value != null)
        {
            details.TryAdd("identificationNumber", ["Ya existe una persona con la misma cédula"]);
        }

        if (!string.IsNullOrWhiteSpace(createPersonDto.Email))
        {
            var isExistingEmailResult = await _personRepository.GetAsync(p => p.Email == createPersonDto.Email);
            if (isExistingEmailResult.IsSuccess && isExistingEmailResult.Value != null)
            {
                details.TryAdd("email", ["Ya existe una persona con el mismo email"]);
            }
        }

        if (details.Count > 0)
        {
            var title = "Error al crear a la Persona";
            return HttpResult<PersonDTO>.Failure(title, details, HttpStatusCode.BadRequest);
        }

        var person = _mapper.Map<Person>(createPersonDto);
        var result = await _personRepository.CreateAsync(person);
        if (!result.IsSuccess)
        {
            var title = "Error al crear a la Persona";
            details.TryAdd("errors", ["No se pudo crear a la Persona"]);
            return HttpResult<PersonDTO>.Failure(title, details, HttpStatusCode.BadRequest);
        }
        var newPerson = result.Value;
        var personDto = _mapper.Map<PersonDTO>(newPerson);

        return HttpResult<PersonDTO>.Success(personDto, HttpStatusCode.Created);
    }

    public async Task<IHttpResult<PersonDTO>> UpdateAsync(
        Guid id,
        UpdatePersonDTO updatePersonDto
    )
    {
        var details = new Dictionary<string, IEnumerable<string>>();

        var existsResult = await _personRepository.AnyAsync(p => p.Id == id);
        if (!existsResult.IsSuccess || !existsResult.Value)
        {
            var title = "Persona no encontrada";
            details.TryAdd("errors", ["No se encontró una persona con el ID proporcionado"]);
            return HttpResult<PersonDTO>.Failure(title, details, HttpStatusCode.NotFound);
        }

        if (!string.IsNullOrWhiteSpace(updatePersonDto.IdentificationNumber))
        {
            var identificationExistsResult = await _personRepository.GetAsync(
                p => p.IdentificationNumber == updatePersonDto.IdentificationNumber && p.Id != id
            );
            if (!identificationExistsResult.IsSuccess)
            {
                details.TryAdd("errors", ["No se pudo verificar si el número de identificación ya está registrado"]);
            }
            else if (identificationExistsResult.Value != null)
            {
                details.TryAdd("identificationNumber", ["Ya existe una persona con el mismo número de identificación"]);
            }
        }

        if (!string.IsNullOrWhiteSpace(updatePersonDto.Email))
        {
            var emailExistsResult = await _personRepository.GetAsync(
                p => p.Email == updatePersonDto.Email && p.Id != id
            );
            if (!emailExistsResult.IsSuccess)
            {
                details.TryAdd("errors", ["No se pudo verificar si el email ya está registrado"]);
            }
            else if (emailExistsResult.Value != null)
            {
                details.TryAdd("email", ["Ya existe una persona con el mismo email"]);
            }
        }

        if (details.Count > 0)
        {
            var title = "No se pudo actualizar a la persona";
            return HttpResult<PersonDTO>.Failure(title, details, HttpStatusCode.BadRequest);
        }

        var personToUpdate = _mapper.Map<Person>(updatePersonDto);
        personToUpdate.Id = id;

        var updateResult = await _personRepository.PartialUpdateAsync(p => p.Id == id, personToUpdate);
        if (!updateResult.IsSuccess)
        {
            var title = "Error al actualizar a la Persona";
            details.TryAdd("errors", ["Ocurrió un error al actualizar la persona"]);
            return HttpResult<PersonDTO>.Failure(title, details, HttpStatusCode.InternalServerError);
        }

        var updatedPersonResult = await _personRepository.GetAsync(p => p.Id == id);
        var updatedPerson = updatedPersonResult.Value;
        var personDTO = _mapper.Map<PersonDTO>(updatedPerson);
        return HttpResult<PersonDTO>.Success(personDTO, HttpStatusCode.OK);
    }

    public async Task<IHttpResult<PersonDTO>> GetAsyncByCedula(string cedula)
    {
        var result = await _personRepository.GetAsync(p => p.IdentificationNumber == cedula);
        if (!result.IsSuccess)
        {
            var title = "Error al obtener a la Persona";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se pudo encontrar a la Persona"] } };
            return HttpResult<PersonDTO>.Failure(title, detail, HttpStatusCode.BadRequest);
        }
        if (result.Value == null)
        {
            var title = "Persona no encontrada";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se encontró una persona con la cédula proporcionada"] } };
            return HttpResult<PersonDTO>.Failure(title, detail, HttpStatusCode.NotFound);
        }
        var person = result.Value;
        var personDto = _mapper.Map<PersonDTO>(person);

        return HttpResult<PersonDTO>.Success(personDto, HttpStatusCode.OK);

    }

    public async Task<IHttpResult<PersonDTO>> UpdateProfilePhotoAsync(Guid id, Guid profilePhotoId)
    {
        var existsResult = await _personRepository.AnyAsync(p => p.Id == id);
        if (!existsResult.IsSuccess || !existsResult.Value)
        {
            var title = "Persona no encontrada";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["No se encontró una persona con el ID proporcionado"] } };
            return HttpResult<PersonDTO>.Failure(title, detail, HttpStatusCode.NotFound);
        }

        var personToUpdate = _mapper.Map<Person>(new UpdatePersonDTO { ProfilePhotoId = profilePhotoId });
        personToUpdate.Id = id;

        var updateResult = await _personRepository.PartialUpdateAsync(p => p.Id == id, personToUpdate);
        if (!updateResult.IsSuccess)
        {
            var title = "Error al actualizar la foto de perfil";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["Ocurrió un error al actualizar la foto de perfil"] } };
            return HttpResult<PersonDTO>.Failure(title, detail, HttpStatusCode.InternalServerError);
        }

        var updatedPerson = updateResult.Value;
        var personDto = _mapper.Map<PersonDTO>(updatedPerson);
        return HttpResult<PersonDTO>.Success(personDto, HttpStatusCode.OK);
    }

    public async Task<IHttpResult<IEnumerable<PersonDTO>>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        if (ids == null || !ids.Any())
        {
            var title = "IDs inválidos";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["Debe proporcionar al menos un ID"] } };
            return HttpResult<IEnumerable<PersonDTO>>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var idsSet = ids.ToHashSet();
        var result = await _personRepository.FilterAsync(p => idsSet.Contains(p.Id));

        if (!result.IsSuccess)
        {
            var title = "Error al obtener las Personas";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["Ocurrió un error al obtener las Personas"] } };
            return HttpResult<IEnumerable<PersonDTO>>.Failure(title, detail, HttpStatusCode.InternalServerError);
        }

        var persons = result.Value;
        var personDtos = _mapper.Map<IEnumerable<PersonDTO>>(persons);
        return HttpResult<IEnumerable<PersonDTO>>.Success(personDtos, HttpStatusCode.OK);
    }

    public async Task<IHttpResult<PagedResponseDTO<PersonDTO>>> SearchAsync(PaginationFilterDTO paginationFilter, HttpContext httpContext, string searchTerm, IEnumerable<Guid>? ids = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            var title = "Término de búsqueda inválido";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["Debe proporcionar un término de búsqueda"] } };
            return HttpResult<PagedResponseDTO<PersonDTO>>.Failure(title, detail, HttpStatusCode.BadRequest);
        }

        var pageNumber = paginationFilter.PageNumber < 1 ? 1 : paginationFilter.PageNumber;
        var pageSize = paginationFilter.PageSize;

        var idsSet = ids?.ToHashSet();

        Expression<Func<Person, bool>> idsFilter = p => idsSet == null || idsSet.Contains(p.Id);
        Expression<Func<Person, bool>> nameFilter = BuildNameSearchPredicate(searchTerm);

        var filter = idsFilter.AndAlsoExpr(nameFilter);

        var countResult = await _personRepository.CountAsync(filter);
        var totalCount = countResult.Value;

        var result = await _personRepository.FilterAsync(
            filter,
            query =>
                query
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
        );

        if (!result.IsSuccess)
        {
            var title = "Error al buscar Personas";
            var detail = new Dictionary<string, IEnumerable<string>> { { "errors", ["Ocurrió un error al buscar las Personas"] } };
            return HttpResult<PagedResponseDTO<PersonDTO>>.Failure(title, detail, HttpStatusCode.InternalServerError);
        }

        var persons = result.Value;
        var personDtos = _mapper.Map<IEnumerable<PersonDTO>>(persons);

        var pagedResponse = _paginationService.BuildPaginationUrl(httpContext, pageNumber, pageSize, totalCount, personDtos);

        return HttpResult<PagedResponseDTO<PersonDTO>>.Success(pagedResponse);
    }
}
