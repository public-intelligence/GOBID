using GOBID.Application.DTOs.Auth;
using GOBID.Application.DTOs.Pds;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using GOBID.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace GOBID.Infrastructure.Services;

public class PdsApiService : IPdsApiService
{
    private const string M2MTokenCacheKey = "pds_m2m_token";

    private readonly HttpClient _httpClient;
    private readonly ILogger<PdsApiService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthService _authService;
    private readonly IMemoryCache _cache;
    private readonly Guid _m2mClientId;
    private readonly string _m2mClientSecret;

    public PdsApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PdsApiService> logger,
        IAuthService authService,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _authService = authService;
        _cache = cache;

        var baseUrl = configuration.GetRequired("PdsApiSettings:BaseUrl");

        _m2mClientId = Guid.Parse(configuration.GetRequired("PdsApiSettings:M2MClientId"));

        _m2mClientSecret = configuration.GetRequired("PdsApiSettings:M2MClientSecret");

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task<string> GetM2MTokenAsync()
    {
        if (_cache.TryGetValue(M2MTokenCacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;

        var result = await _authService.AppLoginAsync(new AppLoginRequestDto
        {
            ClientId = _m2mClientId,
            ClientSecret = _m2mClientSecret
        });

        if (!result.Succeeded || result.Data is null)
        {
            _logger.LogError("No se pudo obtener el token M2M para PDS: {Message}", result.Message);
            throw new InvalidOperationException($"Fallo la autenticación M2M con PDS: {result.Message}");
        }

        var token = result.Data.AccessToken;
        _cache.Set(M2MTokenCacheKey, token, TimeSpan.FromSeconds(result.Data.ExpiresIn - 60));
        return token;
    }

    // Reenvía el Bearer token del request entrante hacia PDS.
    // PDS valida JWTs emitidos por GOBID, por lo que el token del usuario
    // es directamente aceptado por los endpoints de PDS.
    private void AttachBearerToken()
    {
        var authHeader = _httpContextAccessor.HttpContext?
            .Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(authHeader)
            && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..];
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // Adjunta el token M2M como header "Authorization: Bearer". Los endpoints M2M
    // de PDS validan el JWT (claims is_service_account + permissions) desde el
    // header; ya no se acepta el token por query string (?token=).
    private async Task AttachM2MTokenAsync()
    {
        var m2mToken = await GetM2MTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", m2mToken);
    }

    public async Task<Response<PdsPositionDto>> GetPositionAsync(int organizationId, Guid positionId)
    {
        try
        {
            await AttachM2MTokenAsync();

            var url = $"m2m/organizations/{organizationId}/positions/{positionId}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PDS GetPosition M2M org={OrgId} id={Id} → {Status}: {Body}",
                    organizationId, positionId, (int)response.StatusCode, error);
                return new Response<PdsPositionDto>("No se pudo obtener el cargo desde PDS", false);
            }

            var content = await response.Content.ReadAsStringAsync();
            var dto = JsonConvert.DeserializeObject<PdsPositionDto>(content);
            return new Response<PdsPositionDto>(dto, "Ok");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cargo org={OrgId} id={Id} desde PDS", organizationId, positionId);
            return new Response<PdsPositionDto>("Error al obtener el cargo desde PDS", false);
        }
    }

    public async Task<PagedResponse<PdsPositionDto>> SearchPositionsAsync(
        int organizationId, string? q, int? pageNumber, int? pageSize)
    {
        if (pageNumber is null or <= 0) pageNumber = 1;
        if (pageSize is null or <= 0 or > 100) pageSize = 20;

        try
        {
            await AttachM2MTokenAsync();

            var queryParts = new List<string>
            {
                $"pageNumber={pageNumber}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(q))
                queryParts.Add($"q={Uri.EscapeDataString(q)}");

            var response = await _httpClient
                .GetAsync($"m2m/organizations/{organizationId}/positions/search?{string.Join("&", queryParts)}");

            var totalCount = ReadIntHeader(response, "X-Total");
            var totalPages = ReadIntHeader(response, "X-Page-Count");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PDS SearchPositions M2M org={OrgId} → {Status}", organizationId, (int)response.StatusCode);
                return new PagedResponse<PdsPositionDto>([], "No se pudo buscar cargos en PDS");
            }

            var content = await response.Content.ReadAsStringAsync();
            var items = JsonConvert.DeserializeObject<List<PdsPositionDto>>(content) ?? [];
            return new PagedResponse<PdsPositionDto>(items, pageNumber.Value, pageSize.Value, totalCount, totalPages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar cargos en PDS org={OrgId}", organizationId);
            return new PagedResponse<PdsPositionDto>([], "Error al buscar cargos en PDS");
        }
    }

    // Busca las posiciones (cargos) que pertenecen a una estructura organizacional (departamento)
    // específica de PDS. Se usa, entre otras cosas, para resolver a qué personal/usuarios notificar
    // cuando una solicitud entra a un departamento (SGSC no tiene esta granularidad localmente).
    public async Task<PagedResponse<PdsPositionDto>> SearchOrganizationStructurePositionsAsync(
        int organizationId, Guid structureId, string? q, int? pageNumber, int? pageSize, bool includeDescendants = false)
    {
        if (pageNumber is null or <= 0) pageNumber = 1;
        if (pageSize is null or <= 0 or > 100) pageSize = 20;

        try
        {
            await AttachM2MTokenAsync();

            var queryParts = new List<string>
            {
                $"pageNumber={pageNumber}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(q))
                queryParts.Add($"q={Uri.EscapeDataString(q)}");

            if (includeDescendants)
                queryParts.Add("includeDescendants=true");

            var url = $"m2m/organization/{organizationId}/structures/{structureId}/positions/search?{string.Join("&", queryParts)}";
            var response = await _httpClient.GetAsync(url);

            var totalCount = ReadIntHeader(response, "X-Total");
            var totalPages = ReadIntHeader(response, "X-Page-Count");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PDS SearchOrganizationStructurePositions M2M org={OrgId} structure={StructureId} → {Status}: {Body}",
                    organizationId, structureId, (int)response.StatusCode, error);
                return new PagedResponse<PdsPositionDto>([], "No se pudo buscar las posiciones de la estructura organizacional en PDS");
            }

            var content = await response.Content.ReadAsStringAsync();
            var items = JsonConvert.DeserializeObject<List<PdsPositionDto>>(content) ?? [];
            return new PagedResponse<PdsPositionDto>(items, pageNumber.Value, pageSize.Value, totalCount, totalPages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar posiciones de la estructura organizacional org={OrgId} structure={StructureId} en PDS",
                organizationId, structureId);
            return new PagedResponse<PdsPositionDto>([], "Error al buscar las posiciones de la estructura organizacional en PDS");
        }
    }

    public async Task<PagedResponse<PdsPositionDto>> GetPositionsAsync(
        int organizationId, int? pageNumber, int? pageSize)
    {
        if (pageNumber is null or <= 0) pageNumber = 1;
        if (pageSize is null or <= 0 or > 100) pageSize = 20;

        try
        {
            await AttachM2MTokenAsync();

            var url = $"m2m/organizations/{organizationId}/positions" +
                      $"?pageNumber={pageNumber}&pageSize={pageSize}";

            var response = await _httpClient.GetAsync(url);

            var totalCount = ReadIntHeader(response, "X-Total");
            var totalPages = ReadIntHeader(response, "X-Page-Count");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("PDS GetPositions M2M org={OrgId} → {Status}",
                    organizationId, (int)response.StatusCode);
                return new PagedResponse<PdsPositionDto>([], "No se pudo listar cargos desde PDS");
            }

            var content = await response.Content.ReadAsStringAsync();
            var items = JsonConvert.DeserializeObject<List<PdsPositionDto>>(content) ?? [];
            return new PagedResponse<PdsPositionDto>(items, pageNumber.Value, pageSize.Value, totalCount, totalPages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar cargos desde PDS (org={OrgId})", organizationId);
            return new PagedResponse<PdsPositionDto>([], "Error al listar cargos desde PDS");
        }
    }

    public async Task<Response<List<PdsPositionSupervisorLevelDto>>> GetPositionSupervisorChainAsync(
        int organizationId, IEnumerable<Guid> positionIds)
    {
        try
        {
            await AttachM2MTokenAsync();

            var queryParts = positionIds.Select(id => $"positionIds={id}").ToList();

            var url = $"m2m/organizations/{organizationId}/positions/supervisors?{string.Join("&", queryParts)}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PDS GetPositionSupervisorChain M2M org={OrgId} → {Status}: {Body}",
                    organizationId, (int)response.StatusCode, error);
                return new Response<List<PdsPositionSupervisorLevelDto>>(
                    "No se pudo obtener la cadena de supervisores desde PDS", false);
            }

            var content = await response.Content.ReadAsStringAsync();
            var levels = JsonConvert.DeserializeObject<List<PdsPositionSupervisorLevelDto>>(content) ?? [];
            return new Response<List<PdsPositionSupervisorLevelDto>>(levels, "Ok");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cadena de supervisores org={OrgId} desde PDS", organizationId);
            return new Response<List<PdsPositionSupervisorLevelDto>>(
                "Error al obtener la cadena de supervisores desde PDS", false);
        }
    }

    public async Task<Response<List<PdsPositionSupervisorLevelDto>>> GetSubordinatePositionsAsync(
        int organizationId, Guid supervisorPositionId)
    {
        try
        {
            await AttachM2MTokenAsync();

            var url = $"m2m/organizations/{organizationId}/positions/{supervisorPositionId}/subordinates";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PDS GetSubordinatePositions M2M org={OrgId} supervisor={Id} → {Status}: {Body}",
                    organizationId, supervisorPositionId, (int)response.StatusCode, errorBody);
                return new Response<List<PdsPositionSupervisorLevelDto>>(
                    ExtractPdsErrorTitle(errorBody, "No se pudo obtener los cargos subordinados desde PDS"), false);
            }

            var content = await response.Content.ReadAsStringAsync();
            var levels = JsonConvert.DeserializeObject<List<PdsPositionSupervisorLevelDto>>(content) ?? [];
            return new Response<List<PdsPositionSupervisorLevelDto>>(levels, "Ok");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cargos subordinados org={OrgId} supervisor={Id} desde PDS",
                organizationId, supervisorPositionId);
            return new Response<List<PdsPositionSupervisorLevelDto>>(
                "Error al obtener los cargos subordinados desde PDS", false);
        }
    }

    public async Task<Response<List<PdsPositionDto>>> GetPositionsByIdsAsync(
        int organizationId, IEnumerable<Guid> positionIds)
    {
        try
        {
            await AttachM2MTokenAsync();

            var queryParts = positionIds.Select(id => $"positionIds={id}").ToList();

            var url = $"m2m/organizations/{organizationId}/positions/by-ids?{string.Join("&", queryParts)}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PDS GetPositionsByIds M2M org={OrgId} → {Status}: {Body}",
                    organizationId, (int)response.StatusCode, error);
                return new Response<List<PdsPositionDto>>(
                    "No se pudo obtener las posiciones desde PDS", false);
            }

            var content = await response.Content.ReadAsStringAsync();
            var positions = JsonConvert.DeserializeObject<List<PdsPositionDto>>(content) ?? [];
            return new Response<List<PdsPositionDto>>(positions, "Ok");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener posiciones por IDs org={OrgId} desde PDS", organizationId);
            return new Response<List<PdsPositionDto>>(
                "Error al obtener las posiciones por IDs desde PDS", false);
        }
    }

    // Bulk, sin paginar: los IDs de todas las posiciones de una estructura organizacional
    // (y, si includeDescendants es true, de sus estructuras descendientes). Se usa para
    // cruzar roles con posiciones de un área completa (RoleService.ListByStructureAreaAsync),
    // que solo necesita hacer matching por ID.
    public async Task<Response<List<Guid>>> GetPositionIdsByStructureAsync(
        int organizationId, Guid structureId, bool includeDescendants = false)
    {
        try
        {
            await AttachM2MTokenAsync();

            var url = $"m2m/organizations/{organizationId}/structures/{structureId}/positions?includeDescendants={includeDescendants}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PDS GetPositionIdsByStructure M2M org={OrgId} structure={StructureId} → {Status}: {Body}",
                    organizationId, structureId, (int)response.StatusCode, error);
                return new Response<List<Guid>>(
                    "No se pudo obtener las posiciones de la estructura organizacional desde PDS", false);
            }

            var content = await response.Content.ReadAsStringAsync();
            var positionIds = JsonConvert.DeserializeObject<List<Guid>>(content) ?? [];
            return new Response<List<Guid>>(positionIds, "Ok");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener posiciones de la estructura organizacional org={OrgId} structure={StructureId} desde PDS",
                organizationId, structureId);
            return new Response<List<Guid>>(
                "Error al obtener las posiciones de la estructura organizacional desde PDS", false);
        }
    }

    public async Task<Response<PdsPositionDto?>> GetDirectManagerByPositionAsync(int organizationId, Guid positionId)
    {
        try
        {
            await AttachM2MTokenAsync();

            var url = $"m2m/organizations/{organizationId}/positions/{positionId}/direct-manager";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PDS GetDirectManagerByPosition M2M org={OrgId} position={Id} → {Status}: {Body}",
                    organizationId, positionId, (int)response.StatusCode, error);
                return new Response<PdsPositionDto?>("No se pudo obtener el encargado directo desde PDS", false);
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content) || content.Trim() == "null")
                return new Response<PdsPositionDto?>(null, "Ok");

            var dto = JsonConvert.DeserializeObject<PdsPositionDto>(content);
            return new Response<PdsPositionDto?>(dto, "Ok");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener encargado directo org={OrgId} position={Id} desde PDS", organizationId, positionId);
            return new Response<PdsPositionDto?>("Error al obtener el encargado directo desde PDS", false);
        }
    }

    public async Task<PagedResponse<PdsLocalidadDto>> GetLocalidadesAsync(
        int organizationId, PdsLocalidadFilterDto? filter = null, int? pageNumber = null, int? pageSize = null)
    {
        if (pageNumber is null or <= 0) pageNumber = 1;
        if (pageSize is null or <= 0 or > 100) pageSize = 20;

        try
        {
            await AttachM2MTokenAsync();

            var queryParts = new List<string>
            {
                $"pageNumber={pageNumber}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(filter?.Nombre))
                queryParts.Add($"nombre={Uri.EscapeDataString(filter.Nombre)}");
            if (!string.IsNullOrWhiteSpace(filter?.Codigo))
                queryParts.Add($"codigo={Uri.EscapeDataString(filter.Codigo)}");
            if (filter?.TipoId is { } tipoId)
                queryParts.Add($"tipoId={tipoId}");
            if (filter?.EstadoId is { } estadoId)
                queryParts.Add($"estadoId={estadoId}");
            if (filter?.NaturalezaId is { } naturalezaId)
                queryParts.Add($"naturalezaId={naturalezaId}");
            if (filter?.RegionId is { } regionId)
                queryParts.Add($"regionId={regionId}");
            if (filter?.ProvinciaId is { } provinciaId)
                queryParts.Add($"provinciaId={provinciaId}");
            if (filter?.MunicipioId is { } municipioId)
                queryParts.Add($"municipioId={municipioId}");
            if (filter?.UnidadId is { } unidadId)
                queryParts.Add($"unidadId={unidadId}");
            if (filter?.ServicioId is { } servicioId)
                queryParts.Add($"servicioId={servicioId}");
            if (filter?.InstrumentoLegalId is { } instrumentoLegalId)
                queryParts.Add($"instrumentoLegalId={instrumentoLegalId}");

            var url = $"m2m/organization/{organizationId}/locality?{string.Join("&", queryParts)}";
            var response = await _httpClient.GetAsync(url);

            var totalCount = ReadIntHeader(response, "X-Total");
            var totalPages = ReadIntHeader(response, "X-Page-Count");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PDS GetLocalidades M2M org={OrgId} → {Status}: {Body}",
                    organizationId, (int)response.StatusCode, error);
                return new PagedResponse<PdsLocalidadDto>([], "No se pudo listar localidades desde PDS");
            }

            var content = await response.Content.ReadAsStringAsync();
            var items = JsonConvert.DeserializeObject<List<PdsLocalidadDto>>(content) ?? [];
            return new PagedResponse<PdsLocalidadDto>(items, pageNumber.Value, pageSize.Value, totalCount, totalPages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar localidades desde PDS (org={OrgId})", organizationId);
            return new PagedResponse<PdsLocalidadDto>([], "Error al listar localidades desde PDS");
        }
    }

    // Nota: a diferencia de los demás endpoints M2M (org-scoped), este es global
    // (no recibe organizationId), ya que los ids de localidad son únicos globalmente en PDS.
    public async Task<Response<List<PdsLocalidadDto>>> GetLocalidadesByIdsAsync(IEnumerable<Guid> localidadIds)
    {
        try
        {
            await AttachM2MTokenAsync();

            var queryParts = localidadIds.Select(id => $"localidadIds={id}").ToList();
            if (queryParts.Count == 0)
                return new Response<List<PdsLocalidadDto>>([], "Ok");

            var url = $"m2m/localidades/by-ids?{string.Join("&", queryParts)}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("PDS GetLocalidadesByIds M2M → {Status}: {Body}",
                    (int)response.StatusCode, error);
                return new Response<List<PdsLocalidadDto>>("No se pudo obtener las localidades desde PDS", false);
            }

            var content = await response.Content.ReadAsStringAsync();
            var localidades = JsonConvert.DeserializeObject<List<PdsLocalidadDto>>(content) ?? [];
            return new Response<List<PdsLocalidadDto>>(localidades, "Ok");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener localidades por IDs desde PDS");
            return new Response<List<PdsLocalidadDto>>("Error al obtener las localidades por IDs desde PDS", false);
        }
    }

    private static string ExtractPdsErrorTitle(string errorBody, string fallback)
    {
        if (string.IsNullOrWhiteSpace(errorBody)) return fallback;
        try
        {
            var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(errorBody);
            if (obj != null && obj.TryGetValue("title", out var title) && title != null)
                return title.ToString()!;
        }
        catch { }
        return fallback;
    }

    private static int ReadIntHeader(HttpResponseMessage response, string headerName)
    {
        response.Headers.TryGetValues(headerName, out var values);
        return int.TryParse(values?.FirstOrDefault(), out var result) ? result : 0;
    }
}
