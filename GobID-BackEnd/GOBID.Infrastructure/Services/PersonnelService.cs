using GOBID.Application.DTOs.Personnel;
using GOBID.Application.DTOs.Pds;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.ViewModels.Personnel;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using GOBID.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GOBID.Infrastructure.Services;

public class PersonnelService : IPersonnelService
{
    private readonly PrincipalContext _context;
    private readonly IPdsApiService _pdsApiService;
    private readonly IPersonApiService _personApiService;

    public PersonnelService(PrincipalContext context, IPdsApiService pdsApiService, IPersonApiService personApiService)
    {
        _context = context;
        _pdsApiService = pdsApiService;
        _personApiService = personApiService;
    }

    // El cargo efectivo de un personnel: position_id si está informado,
    // position_nominate_id como respaldo cuando no lo está.
    private static Guid? EffectivePositionId(Personnel p) => p.PositionId ?? p.PositionNominateId;

    public async Task<Response<PersonnelVm>> GetAsync(Guid personnelId)
    {
        try
        {
            var entity = await _context.Personnels
                .FirstOrDefaultAsync(p => p.PersonnelId == personnelId);

            if (entity == null)
                return new Response<PersonnelVm>("No se encontró el registro de personal", false);

            PdsPositionDto? position = null;
            PdsLocalidadDto? locality = null;
            IdentityPersonResponseDTO? person = null;

            var effectivePositionId = EffectivePositionId(entity);

            // Obtener información de la Posición desde PDS
            if (effectivePositionId.HasValue)
            {
                try
                {
                    var positionResponse = await _pdsApiService.GetPositionAsync(entity.OrganizationId, effectivePositionId.Value);
                    if (positionResponse.Succeeded && positionResponse.Data != null)
                    {
                        position = positionResponse.Data;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error obteniendo Position: {ex.Message}");
                }
            }

            // Obtener información de la Localidad desde PDS
            if (entity.LocalityId.HasValue)
            {
                try
                {
                    var localidadResponse = await _pdsApiService.GetLocalidadesByIdsAsync([entity.LocalityId.Value]);
                    if (localidadResponse.Succeeded && localidadResponse.Data != null)
                    {
                        locality = localidadResponse.Data.FirstOrDefault();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error obteniendo Localidad: {ex.Message}");
                }
            }

            // Obtener información de la Persona desde Identity
            try
            {
                var personResponse = await _personApiService.GetPersonByIdAsync(entity.PersonId);
                if (personResponse.Succeeded && personResponse.Data != null)
                {
                    person = personResponse.Data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo Person: {ex.Message}");
            }

            return new Response<PersonnelVm>(ToVm(entity, position, person, locality), "Ok");
        }
        catch (Exception ex)
        {
            return new Response<PersonnelVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<PagedResponse<PersonnelVm>> ListAsync(int organizationId, int? pageNumber, int? pageSize, bool? isActive, Guid? positionId, int? roleId, string? searchTerm)
    {
        if (pageNumber is null or <= 0) pageNumber = 1;
        if (pageSize is null or <= 0 or > 100) pageSize = 50;

        try
        {
            var query = _context.Personnels.Where(p => p.OrganizationId == organizationId);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            if (positionId.HasValue && roleId > 0)
            {
                return new PagedResponse<PersonnelVm>("Solo se puede filtrar por positionId o por roleId, no ambos", false);
            }

            Guid? effectivePositionFilter = positionId;

            if (roleId > 0)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);
                if (role == null)
                {
                    return new PagedResponse<PersonnelVm>("El rol no existe", false);
                }

                if (role.PositionId == null || role.PositionId == Guid.Empty)
                {
                    return new PagedResponse<PersonnelVm>("El rol no contiene un positionId", false);
                }

                effectivePositionFilter = role.PositionId.Value;
            }

            if (effectivePositionFilter.HasValue)
                query = query.Where(p => (p.PositionId ?? p.PositionNominateId) == effectivePositionFilter.Value);

            List<Personnel> personnels;
            Dictionary<Guid, IdentityPersonResponseDTO> personById;
            int totalCount;
            int totalPages;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // El personal se guarda localmente pero el nombre/cédula de la persona vive en Identity,
                // por lo que la búsqueda se delega a PersonApiService, restringida a los personId de la organización.
                var candidatePersonIds = await query.Select(p => p.PersonId).Distinct().ToListAsync();
                if (candidatePersonIds.Count == 0)
                    return new PagedResponse<PersonnelVm>(new List<PersonnelVm>(), pageNumber.Value, pageSize.Value, 0, 0, "Ok");

                var searchResponse = await _personApiService.SearchPersonsAsync(pageNumber, pageSize, searchTerm, candidatePersonIds);
                if (!searchResponse.Succeeded || searchResponse.Data == null || searchResponse.Data.Count == 0)
                    return new PagedResponse<PersonnelVm>(new List<PersonnelVm>(), pageNumber.Value, pageSize.Value, 0, 0, "Ok");

                // La búsqueda ya trae los datos completos de la persona: se reutilizan más abajo
                // en vez de volver a pedirlos uno por uno con GetPersonByIdAsync.
                personById = searchResponse.Data.ToDictionary(p => p.Id);
                var matchedPersonIds = searchResponse.Data.Select(p => p.Id).ToList();

                personnels = await query
                    .Where(p => matchedPersonIds.Contains(p.PersonId))
                    .ToListAsync();

                // Preservar el orden de relevancia devuelto por la búsqueda de personas.
                var orderIndex = matchedPersonIds
                    .Select((id, idx) => (id, idx))
                    .ToDictionary(x => x.id, x => x.idx);
                personnels = personnels
                    .OrderBy(p => orderIndex.TryGetValue(p.PersonId, out var idx) ? idx : int.MaxValue)
                    .ToList();

                totalCount = searchResponse.TotalCount;
                totalPages = searchResponse.TotalPages;
            }
            else
            {
                totalCount = await query.CountAsync();
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

                personnels = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .ToListAsync();

                // Traer las personas en bulk en lugar de una llamada por cada personnel.
                var personIds = personnels.Select(p => p.PersonId).Distinct().ToList();
                var personsResponse = await _personApiService.GetPersonsByIdsAsync(personIds);
                personById = personsResponse.Succeeded && personsResponse.Data != null
                    ? personsResponse.Data.ToDictionary(p => p.Id)
                    : [];
            }

            var localityById = await GetLocalitiesByIdsAsync(personnels.Select(p => p.LocalityId));

            var list = new List<PersonnelVm>();

            foreach (var personnel in personnels)
            {
                PdsPositionDto? position = null;
                var effectivePositionId = EffectivePositionId(personnel);

                if (effectivePositionId.HasValue)
                {
                    try
                    {
                        var positionResponse = await _pdsApiService.GetPositionAsync(organizationId, effectivePositionId.Value);
                        if (positionResponse.Succeeded && positionResponse.Data != null)
                            position = positionResponse.Data;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error obteniendo Position para PersonnelId {personnel.PersonnelId}: {ex.Message}");
                    }
                }

                var person = personById.TryGetValue(personnel.PersonId, out var p) ? p : null;

                var locality = personnel.LocalityId.HasValue && localityById.TryGetValue(personnel.LocalityId.Value, out var loc)
                    ? loc
                    : null;

                list.Add(ToVm(personnel, position, person, locality));
            }

            return new PagedResponse<PersonnelVm>(list, pageNumber.Value, pageSize.Value, totalCount, totalPages, "Ok");
        }
        catch (Exception ex)
        {
            return new PagedResponse<PersonnelVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<PersonnelVm>> GetByPersonIdAsync(Guid personId)
    {
        try
        {
            var entity = await _context.Personnels
                .Where(p => p.PersonId == personId && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (entity == null)
                return new Response<PersonnelVm>("No se encontró registro de personal para esta persona", false);

            PdsPositionDto? position = null;
            var effectivePositionId = EffectivePositionId(entity);
            if (effectivePositionId.HasValue)
            {
                try
                {
                    var positionResponse = await _pdsApiService.GetPositionAsync(entity.OrganizationId, effectivePositionId.Value);
                    if (positionResponse.Succeeded && positionResponse.Data != null)
                        position = positionResponse.Data;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error obteniendo Position: {ex.Message}");
                }
            }

            PdsLocalidadDto? locality = null;
            if (entity.LocalityId.HasValue)
            {
                try
                {
                    var localidadResponse = await _pdsApiService.GetLocalidadesByIdsAsync([entity.LocalityId.Value]);
                    if (localidadResponse.Succeeded && localidadResponse.Data != null)
                        locality = localidadResponse.Data.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error obteniendo Localidad: {ex.Message}");
                }
            }

            return new Response<PersonnelVm>(ToVm(entity, position, null, locality), "Ok");
        }
        catch (Exception ex)
        {
            return new Response<PersonnelVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<PersonnelVm>> UpsertAsync(PersonnelUpsertDto dto)
    {
        try
        {
            var effectivePositionId = dto.PositionId ?? dto.PositionNominateId;

            if (dto.IsActive)
            {
                // 1. Validar que si la posición es IsManager, no se pueda duplicar el registro activo con esa posición.
                var positionResponse = await _pdsApiService.GetPositionAsync(dto.OrganizationId, effectivePositionId);
                if (!positionResponse.Succeeded || positionResponse.Data == null)
                {
                    return new Response<PersonnelVm>("No se encontró la posición especificada", false);
                }

                if (positionResponse.Data.IsManager)
                {
                    var activeManagerExists = await _context.Personnels
                        .AnyAsync(p => (p.PositionId ?? p.PositionNominateId) == effectivePositionId
                                    && p.IsActive
                                    && (!dto.PersonnelId.HasValue || p.PersonnelId != dto.PersonnelId.Value));

                    if (activeManagerExists)
                    {
                        return new Response<PersonnelVm>("Ya existe un registro de personal activo con esta posición encargado", false);
                    }
                }

                // 2. Validar que no se pueda agregar/actualizar un personId si ya hay un registro activo con el mismo personId.
                var activePersonExists = await _context.Personnels
                    .AnyAsync(p => p.PersonId == dto.PersonId
                                && p.IsActive
                                && (!dto.PersonnelId.HasValue || p.PersonnelId != dto.PersonnelId.Value));

                if (activePersonExists)
                {
                    return new Response<PersonnelVm>("La persona ya tiene un registro de personal activo", false);
                }
            }

            if (dto.PersonnelId.HasValue)
            {
                var entity = await _context.Personnels
                    .FirstOrDefaultAsync(p => p.PersonnelId == dto.PersonnelId.Value);

                if (entity == null)
                    return new Response<PersonnelVm>("No se encontró el registro de personal a actualizar", false);

                entity.PositionId = dto.PositionId;
                entity.LocalityId = dto.LocalityId;
                entity.PositionNominateId = dto.PositionNominateId;
                entity.PersonId = dto.PersonId;
                entity.OrganizationId = dto.OrganizationId;
                entity.IsActive = dto.IsActive;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = dto.UpdatedBy;

                await _context.SaveChangesAsync();
                return new Response<PersonnelVm>(ToVm(entity), "Operación exitosa");
            }
            else
            {
                var entity = new Personnel
                {
                    PositionId = dto.PositionId,
                    LocalityId = dto.LocalityId,
                    PositionNominateId = dto.PositionNominateId,
                    PersonId = dto.PersonId,
                    OrganizationId = dto.OrganizationId,
                    IsActive = dto.IsActive,
                    CreatedBy = dto.CreatedBy
                };

                _context.Personnels.Add(entity);
                await _context.SaveChangesAsync();
                return new Response<PersonnelVm>(ToVm(entity), "Operación exitosa");
            }
        }
        catch (Exception ex)
        {
            return new Response<PersonnelVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<string>> DeleteAsync(Guid personnelId, Guid deletedBy)
    {
        try
        {
            var entity = await _context.Personnels
                .FirstOrDefaultAsync(p => p.PersonnelId == personnelId);

            if (entity == null)
                return new Response<string>("No se encontró el registro de personal", false);

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = deletedBy;

            await _context.SaveChangesAsync();
            return new Response<string>("Operación exitosa", true);
        }
        catch (Exception ex)
        {
            return new Response<string>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<PersonnelSupervisorResultVm>> GetSupervisorPersonnelAsync(
        int organizationId, int roleId)
    {
        try
        {
            // 0. Buscar el rol y verificar si contiene un PositionId
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role == null)
            {
                return new Response<PersonnelSupervisorResultVm>("El rol no existe", false);
            }

            if (role.PositionId == null || role.PositionId == Guid.Empty)
            {
                return new Response<PersonnelSupervisorResultVm>("El rol no contiene un positionId", false);
            }

            Guid positionId = role.PositionId.Value;

            // 1. Obtener jerarquía de supervisores desde PDS
            var chainResult = await _pdsApiService.GetPositionSupervisorChainAsync(organizationId, new List<Guid> { positionId });
            if (!chainResult.Succeeded || chainResult.Data == null || chainResult.Data.Count == 0)
                return new Response<PersonnelSupervisorResultVm>(
                    new PersonnelSupervisorResultVm(), "No se encontraron supervisores en la jerarquía");

            // 2. Recorrer niveles en orden ascendente buscando personal activo
            foreach (var level in chainResult.Data.OrderBy(l => l.Level))
            {
                var levelPositionIds = level.Positions.Select(p => p.Id).ToList();

                var personnelAtLevel = await _context.Personnels
                    .Where(p => p.OrganizationId == organizationId
                             && levelPositionIds.Contains((p.PositionId ?? p.PositionNominateId ?? Guid.Empty))
                             && p.IsActive)
                    .ToListAsync();

                if (personnelAtLevel.Count == 0) continue;

                // 3. Obtener datos de personas en bulk
                var personIds = personnelAtLevel.Select(p => p.PersonId).Distinct().ToList();
                var personsResponse = await _personApiService.GetPersonsByIdsAsync(personIds);
                var personById = personsResponse.Succeeded && personsResponse.Data != null
                    ? personsResponse.Data.ToDictionary(p => p.Id)
                    : [];

                // 4. Mapear el resultado con info de posición (ya disponible de PDS) y persona
                var positionById = level.Positions.ToDictionary(p => p.Id);

                var firstPersonnel = personnelAtLevel.First();
                positionById.TryGetValue(EffectivePositionId(firstPersonnel) ?? Guid.Empty, out var firstPosition);
                personById.TryGetValue(firstPersonnel.PersonId, out var firstPerson);

                var localityById = await GetLocalitiesByIdsAsync([firstPersonnel.LocalityId]);
                var firstLocality = firstPersonnel.LocalityId.HasValue && localityById.TryGetValue(firstPersonnel.LocalityId.Value, out var loc)
                    ? loc
                    : null;

                return new Response<PersonnelSupervisorResultVm>(new PersonnelSupervisorResultVm
                {
                    Level = level.Level,
                    Supervisors = ToVm(firstPersonnel, firstPosition, firstPerson, firstLocality)
                }, "Ok");
            }

            return new Response<PersonnelSupervisorResultVm>(
                new PersonnelSupervisorResultVm(),
                "No se encontró personal activo en la cadena de supervisores");
        }
        catch (Exception ex)
        {
            return new Response<PersonnelSupervisorResultVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<List<PersonnelVm>>> GetSubordinatePersonnelAsync(
    int organizationId, int roleId)
{
    try
    {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role == null)
            {
                return new Response<List<PersonnelVm>>("El rol no existe", false);
            }

            if (role.PositionId == null || role.PositionId == Guid.Empty)
            {
                return new Response<List<PersonnelVm>>("El rol no contiene un positionId", false);
            }

            Guid positionId = role.PositionId.Value;
            // 1. Obtener estructura de supervisión desde PDS
            var levelsResult = await _pdsApiService.GetSubordinatePositionsAsync(organizationId, positionId);
        if (!levelsResult.Succeeded || levelsResult.Data == null)
            return new Response<List<PersonnelVm>>(
                levelsResult.Message ?? "No se pudo obtener la estructura de supervisión",
                false);

        var allSubordinatePositionIds = levelsResult.Data
            .SelectMany(l => l.Positions)
            .Select(p => p.Id)
            .ToHashSet();

        if (allSubordinatePositionIds.Count == 0)
            return new Response<List<PersonnelVm>>(
                new List<PersonnelVm>(),
                "La posición no tiene personal subordinado en la estructura definida.");

        // Mapa de positionId → nivel jerárquico
        var positionIdToLevel = levelsResult.Data
            .SelectMany(l => l.Positions.Select(p => (p.Id, l.Level)))
            .ToDictionary(x => x.Id, x => x.Level);

        // 2. Buscar personal activo que ocupa cualquiera de esos cargos en la organización
        var personnels = await _context.Personnels
            .Where(p => p.OrganizationId == organizationId
                     && allSubordinatePositionIds.Contains((p.PositionId ?? p.PositionNominateId ?? Guid.Empty))
                     && p.IsActive)
            .ToListAsync();

        // 3. Obtener personas en bulk
        var personIds = personnels.Select(p => p.PersonId).Distinct().ToList();
        var personsResponse = await _personApiService.GetPersonsByIdsAsync(personIds);
        var personById = personsResponse.Succeeded && personsResponse.Data != null
            ? personsResponse.Data.ToDictionary(p => p.Id)
            : [];

        // 4. Obtener posiciones en bulk (para tener el nombre/nivel completo si es necesario)
        var positionIds = personnels.Select(EffectivePositionId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var positionsResponse = await _pdsApiService.GetPositionsByIdsAsync(organizationId, positionIds);
        var positionById = positionsResponse.Succeeded && positionsResponse.Data != null
            ? positionsResponse.Data.ToDictionary(p => p.Id)
            : [];

        // 5. Obtener localidades en bulk
        var localityById = await GetLocalitiesByIdsAsync(personnels.Select(p => p.LocalityId));

        // 6. Mapear el resultado final incluyendo el nivel jerárquico
        var resultList = personnels.Select(p =>
        {
            var effectivePositionId = EffectivePositionId(p);
            var locality = p.LocalityId.HasValue && localityById.TryGetValue(p.LocalityId.Value, out var loc) ? loc : null;
            var vm = ToVm(p,
                effectivePositionId.HasValue && positionById.TryGetValue(effectivePositionId.Value, out var pos) ? pos : null,
                personById.TryGetValue(p.PersonId, out var person) ? person : null,
                locality);
            vm.Level = effectivePositionId.HasValue && positionIdToLevel.TryGetValue(effectivePositionId.Value, out var lvl) ? lvl : 0;
            return vm;
        }).ToList();

        return new Response<List<PersonnelVm>>(resultList, "Ok");
    }
    catch (Exception ex)
    {
        return new Response<List<PersonnelVm>>($"Error: {ex.Message}", false);
    }
}

    public async Task<Response<PersonnelSupervisorResultVm>> GetDirectManagerPersonnelAsync(int organizationId, int roleId)
    {
        try
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role == null)
                return new Response<PersonnelSupervisorResultVm>("El rol no existe", false);

            if (role.PositionId == null || role.PositionId == Guid.Empty)
                return new Response<PersonnelSupervisorResultVm>("El rol no contiene un positionId", false);

            Guid positionId = role.PositionId.Value;

            // Obtener la posición del encargado directo desde PDS
            var managerPositionResponse = await _pdsApiService.GetDirectManagerByPositionAsync(organizationId, positionId);
            if (!managerPositionResponse.Succeeded)
                return new Response<PersonnelSupervisorResultVm>(
                    managerPositionResponse.Message ?? "No se pudo obtener el encargado directo", false);

            if (managerPositionResponse.Data == null)
                return new Response<PersonnelSupervisorResultVm>(
                    new PersonnelSupervisorResultVm(),
                    "La posición no tiene un encargado directo definido en la jerarquía");

            var managerPosition = managerPositionResponse.Data;

            // Buscar personal activo asignado a esa posición de encargado
            var personnel = await _context.Personnels
                .FirstOrDefaultAsync(p => p.OrganizationId == organizationId
                                       && (p.PositionId ?? p.PositionNominateId) == managerPosition.Id
                                       && p.IsActive);

            if (personnel == null)
                return new Response<PersonnelSupervisorResultVm>(
                    new PersonnelSupervisorResultVm { Level = 1 },
                    "El encargado directo no tiene personal activo asignado");

            IdentityPersonResponseDTO? person = null;
            try
            {
                var personResponse = await _personApiService.GetPersonByIdAsync(personnel.PersonId);
                if (personResponse.Succeeded && personResponse.Data != null)
                    person = personResponse.Data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo Person: {ex.Message}");
            }

            PdsLocalidadDto? locality = null;
            if (personnel.LocalityId.HasValue)
            {
                try
                {
                    var localidadResponse = await _pdsApiService.GetLocalidadesByIdsAsync([personnel.LocalityId.Value]);
                    if (localidadResponse.Succeeded && localidadResponse.Data != null)
                        locality = localidadResponse.Data.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error obteniendo Localidad: {ex.Message}");
                }
            }

            return new Response<PersonnelSupervisorResultVm>(new PersonnelSupervisorResultVm
            {
                Level = 1,
                Supervisors = ToVm(personnel, managerPosition, person, locality)
            }, "Ok");
        }
        catch (Exception ex)
        {
            return new Response<PersonnelSupervisorResultVm>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<List<Guid>>> GetSubordinateUserIdsByPersonIdAsync(
    int organizationId, Guid personId)
    {
        try
        {
            var personnel = await _context.Personnels.FirstOrDefaultAsync(r => r.PersonId == personId);
            if (personnel == null)
            {
                return new Response<List<Guid>>("El personal no existe", false);
            }

            var positionId = EffectivePositionId(personnel);
            if (!positionId.HasValue)
            {
                return new Response<List<Guid>>("El personal no tiene un positionId ni positionNominateId", false);
            }

            // 1. Obtener estructura de supervisión desde PDS
            var levelsResult = await _pdsApiService.GetSubordinatePositionsAsync(organizationId, positionId.Value);
            if (!levelsResult.Succeeded || levelsResult.Data == null)
                return new Response<List<Guid>>(
                    levelsResult.Message ?? "No se pudo obtener la estructura de supervisión",
                    false);

            var allSubordinatePositionIds = levelsResult.Data
                .SelectMany(l => l.Positions)
                .Select(p => p.Id)
                .ToHashSet();

            if (allSubordinatePositionIds.Count == 0)
                return new Response<List<Guid>>(
                    new List<Guid>(),
                    "La posición no tiene personal subordinado en la estructura definida.");

            // 2. Buscar personal activo que ocupa cualquiera de esos cargos en la organización
            var subordinatePersonnelIds = await _context.Personnels
                .Where(p => p.OrganizationId == organizationId
                         && allSubordinatePositionIds.Contains((p.PositionId ?? p.PositionNominateId ?? Guid.Empty))
                         && p.IsActive)
                .Select(p => p.PersonnelId)
                .ToListAsync();

            if (subordinatePersonnelIds.Count == 0)
                return new Response<List<Guid>>(new List<Guid>(), "Ok");

            // 3. Resolver los userId asociados a ese personal (User.PersonnelId → Personnel.PersonnelId)
            var userIds = await _context.Users
                .Where(u => subordinatePersonnelIds.Contains((Guid)u.PersonPositionId) && u.IsActive)
                .Select(u => u.UserId)
                .Distinct()
                .ToListAsync();

            return new Response<List<Guid>>(userIds, "Ok");
        }
        catch (Exception ex)
        {
            return new Response<List<Guid>>($"Error: {ex.Message}", false);
        }
    }

    public async Task<Response<List<UserContactDto>>> GetUserEmailsByOrganizationStructureAsync(int organizationId, Guid structureId)
    {
        try
        {
            // 1. Obtener (paginando hasta agotar) todas las posiciones de la estructura organizacional en PDS
            var positionIds = new List<Guid>();
            var pageNumber = 1;
            const int pageSize = 100;
            var totalPages = 1;

            do
            {
                var positionsResult = await _pdsApiService.SearchOrganizationStructurePositionsAsync(
                    organizationId, structureId, null, pageNumber, pageSize);

                if (!positionsResult.Succeeded)
                    return new Response<List<UserContactDto>>(
                        positionsResult.Message ?? "No se pudieron obtener las posiciones de la estructura organizacional", false);

                if (positionsResult.Data == null || positionsResult.Data.Count == 0)
                    break;

                positionIds.AddRange(positionsResult.Data.Select(p => p.Id));
                totalPages = positionsResult.TotalPages;
                pageNumber++;
            } while (pageNumber <= totalPages);

            if (positionIds.Count == 0)
                return new Response<List<UserContactDto>>(new List<UserContactDto>(), "La estructura organizacional no tiene posiciones asociadas");

            // 2. Buscar el personal activo de la organización que ocupa alguna de esas posiciones
            var positionIdSet = positionIds.ToHashSet();
            var personnelIds = await _context.Personnels
                .Where(p => p.OrganizationId == organizationId
                         && positionIdSet.Contains((p.PositionId ?? p.PositionNominateId ?? Guid.Empty))
                         && p.IsActive)
                .Select(p => p.PersonnelId)
                .ToListAsync();

            if (personnelIds.Count == 0)
                return new Response<List<UserContactDto>>(new List<UserContactDto>(), "No hay personal activo asignado a las posiciones de esta estructura");

            // 3. Resolver identidad (UserId) + correo desde la tabla User (User.PersonPositionId → Personnel.PersonnelId)
            var users = await _context.Users
                .Where(u => u.PersonPositionId.HasValue
                         && personnelIds.Contains(u.PersonPositionId.Value)
                         && u.IsActive)
                .Select(u => new UserContactDto { UserId = u.UserId, Email = u.Email })
                .Distinct()
                .ToListAsync();

            return new Response<List<UserContactDto>>(users, "Ok");
        }
        catch (Exception ex)
        {
            return new Response<List<UserContactDto>>($"Error: {ex.Message}", false);
        }
    }

    // Resuelve en bulk las localidades de PDS para un conjunto de LocalityId (nulls ignorados).
    private async Task<Dictionary<Guid, PdsLocalidadDto>> GetLocalitiesByIdsAsync(IEnumerable<Guid?> localityIds)
    {
        var ids = localityIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0)
            return [];

        try
        {
            var response = await _pdsApiService.GetLocalidadesByIdsAsync(ids);
            return response.Succeeded && response.Data != null
                ? response.Data.ToDictionary(l => l.Id)
                : [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error obteniendo Localidades en bulk: {ex.Message}");
            return [];
        }
    }

    private PersonnelVm ToVm(Personnel p, PdsPositionDto? position = null, IdentityPersonResponseDTO? person = null, PdsLocalidadDto? locality = null) => new()
    {
        PersonnelId = p.PersonnelId,
        PersonId = p.PersonId,
        PositionId = p.PositionId,
        LocalityId = p.LocalityId,
        PositionNominateId = p.PositionNominateId,
        OrganizationId = p.OrganizationId,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Position = position,
        Locality = locality,
        Person = person
    };
}
