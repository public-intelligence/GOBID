using GOBID.Application.DTOs.Auth;
using GOBID.Application.DTOs.Personnel;
using GOBID.Application.Enums;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using GOBID.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace GOBID.Infrastructure.Services;

public class PersonApiService : IPersonApiService
{
    private const string DmsM2MTokenCacheKey = "dms_m2m_token";
    private const string DmsUnavailableMessage = "El servicio de documentos (DMS) no está disponible en este momento.";

    // Timeout corto para que un DMS caído no retenga la solicitud los 100s por defecto de HttpClient
    private static readonly TimeSpan DmsTimeout = TimeSpan.FromSeconds(15);

    private readonly HttpClient _httpClient;
    private readonly ILogger<PersonApiService> _logger;
    private readonly string _baseUrl;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;
    private readonly string? _dmsBaseUrl;
    private readonly Guid _dmsClientId;
    private readonly string? _dmsClientSecret;
    private readonly string? _dmsEnvironmentId;
    private readonly bool _isDmsConfigured;

    public PersonApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<PersonApiService> logger,
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;

        // Configurar desde appsettings
        _baseUrl = configuration.GetRequired("PersonApiSettings:BaseUrl");

        // DMS es opcional: si falta su configuración, el servicio se construye igual y sólo
        // las operaciones de foto responden con error, sin interrumpir el resto del flujo.
        _dmsBaseUrl = configuration["DmsApiSettings:BaseUrl"];

        _dmsClientId = Guid.TryParse(configuration["DmsApiSettings:ClientId"], out var dmsClientId)
            ? dmsClientId
            : Guid.Empty;

        _dmsClientSecret = configuration["DmsApiSettings:ClientSecret"];

        // ID de entorno fijo asignado a GOBID dentro del DMS (X-Environment-ID)
        _dmsEnvironmentId = configuration["DmsApiSettings:EnvironmentId"];

        _isDmsConfigured = Uri.TryCreate(_dmsBaseUrl, UriKind.Absolute, out _)
            && _dmsClientId != Guid.Empty
            && !string.IsNullOrWhiteSpace(_dmsClientSecret)
            && !string.IsNullOrWhiteSpace(_dmsEnvironmentId);

        if (!_isDmsConfigured)
            _logger.LogWarning("DmsApiSettings incompleto: las operaciones de foto de perfil con el DMS estarán deshabilitadas.");

        // Configurar HttpClient
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task<string> GetDmsM2MTokenAsync()
    {
        if (_cache.TryGetValue(DmsM2MTokenCacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;

        // IAuthService se resuelve en un scope aparte (y no se inyecta directo en el constructor)
        // porque AuthService depende de IPersonApiService: inyectarlo de forma directa aquí
        // crearía una dependencia circular (PersonApiService -> IAuthService -> IPersonApiService).
        using var scope = _scopeFactory.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var result = await authService.AppLoginAsync(new AppLoginRequestDto
        {
            ClientId = _dmsClientId,
            ClientSecret = _dmsClientSecret!
        });

        if (!result.Succeeded || result.Data is null)
        {
            _logger.LogError("No se pudo obtener el token M2M para DMS: {Message}", result.Message);
            throw new InvalidOperationException($"Fallo la autenticación M2M con DMS: {result.Message}");
        }

        var token = result.Data.AccessToken;
        _cache.Set(DmsM2MTokenCacheKey, token, TimeSpan.FromSeconds(result.Data.ExpiresIn - 60));
        return token;
    }

    private async Task<HttpClient> GetAuthenticatedDmsClientAsync()
    {
        var token = await GetDmsM2MTokenAsync();
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_dmsBaseUrl!);
        client.Timeout = DmsTimeout;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Agrega los headers X-Actor-* que exige el DMS para operaciones iniciadas por un
    /// usuario (citizen/officer). Identifican quién está detrás de la llamada, más allá
    /// del X-Environment-ID de la aplicación. targetPersonId es la persona sobre la que
    /// se opera (dueña de la foto), no necesariamente quien hace la llamada.
    /// </summary>
    private void AddActorHeaders(HttpRequestHeaders headers, Guid targetPersonId, string operation)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        var isServiceAccount = user?.FindFirst("is_service_account")?.Value == "true";

        var subjectId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value
            ?? "gobid-system";

        var actorPersonId = user?.FindFirst("person_id")?.Value ?? subjectId;
        var displayName = user?.FindFirst("person_full_name")?.Value ?? "GOBID System";
        var actorType = isServiceAccount ? "system" : "user";
        var caseRole = user?.FindFirst("role_name")?.Value ?? "gobid:m2m";
        var orgId = user?.FindFirst("organization_id")?.Value ?? string.Empty;

        headers.TryAddWithoutValidation("X-Actor-Subject-Id", subjectId);
        headers.TryAddWithoutValidation("X-Actor-Person-Id", actorPersonId);
        headers.TryAddWithoutValidation("X-Actor-Display-Name", Uri.EscapeDataString(displayName));
        headers.TryAddWithoutValidation("X-Actor-Type", actorType);
        headers.TryAddWithoutValidation("X-Actor-Case-Role", caseRole);
        headers.TryAddWithoutValidation("X-Actor-Organization-Id", orgId);
        headers.TryAddWithoutValidation("X-Actor-Application-Id", _dmsEnvironmentId);
        headers.TryAddWithoutValidation("X-Actor-Case-Id", targetPersonId.ToString());
        headers.TryAddWithoutValidation("X-Actor-Service-Request-Id", "all");
        headers.TryAddWithoutValidation("X-Actor-Operation", operation);
    }

    /// <summary>
    /// Fallos de red, timeout o de autenticación M2M: el DMS no responde y no hay nada
    /// que el llamador pueda corregir.
    /// </summary>
    private static bool IsDmsUnavailable(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or InvalidOperationException;

    private string ExtractErrorMessage(string errorContent)
    {
        try
        {
            var errorObj = JsonConvert.DeserializeObject<dynamic>(errorContent);
            if (errorObj == null)
                return errorContent;

            if (errorObj.detail != null)
            {
                if (errorObj.detail is Newtonsoft.Json.Linq.JObject detailObj)
                {
                    var errorMessages = new List<string>();
                    foreach (var prop in detailObj.Properties())
                    {
                        var value = prop.Value;
                        if (value is Newtonsoft.Json.Linq.JArray array)
                        {
                            foreach (var item in array)
                            {
                                errorMessages.Add(item.ToString());
                            }
                        }
                        else
                        {
                            errorMessages.Add($"{prop.Name}: {value}");
                        }
                    }
                    return string.Join(". ", errorMessages);
                }
                return errorObj.detail.ToString();
            }

            string? message = errorObj.message?.ToString();
            string? title = errorObj.title?.ToString();
            string? error = errorObj.error?.ToString();

            if (!string.IsNullOrEmpty(message))
                return message;

            if (!string.IsNullOrEmpty(title))
                return title;

            if (!string.IsNullOrEmpty(error))
                return error;

            if (errorObj.errors != null)
                return JsonConvert.SerializeObject(errorObj.errors);

            return errorContent;
        }
        catch
        {
            return errorContent;
        }
    }

    public async Task<Response<IdentityPersonResponseDTO>> CreatePersonAsync(CreatePersonDTO createPersonDto)
    {
        try
        {
            var dtoToSerialize = createPersonDto with
            {
                Email = string.IsNullOrWhiteSpace(createPersonDto.Email) ? null : createPersonDto.Email,
                PhoneNumber = string.IsNullOrWhiteSpace(createPersonDto.PhoneNumber) ? null : createPersonDto.PhoneNumber
            };
            var json = JsonConvert.SerializeObject(dtoToSerialize, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("person", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = ExtractErrorMessage(errorContent);
                return new Response<IdentityPersonResponseDTO>(
                    $"Error al crear la persona: {errorMessage}",
                    false);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var person = JsonConvert.DeserializeObject<IdentityPersonResponseDTO>(responseContent);
            return new Response<IdentityPersonResponseDTO>(person, "Persona creada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear persona");
            return new Response<IdentityPersonResponseDTO>($"Error al crear la persona", false);
        }
    }

    public async Task<Response<IdentityPersonResponseDTO>> GetPersonByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"person/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = ExtractErrorMessage(errorContent);
                return new Response<IdentityPersonResponseDTO>($"Error al obtener la persona: {errorMessage}", false);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var person = JsonConvert.DeserializeObject<IdentityPersonResponseDTO>(responseContent);
            return new Response<IdentityPersonResponseDTO>(person, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener persona");
            return new Response<IdentityPersonResponseDTO>($"Error al obtener la persona", false);
        }
    }

    public async Task<Response<IdentityPersonResponseDTO>> GetPersonByCedulaAsync(string identificationNumber, bool isPasssport)
    {
        try
        {
            var response = await _httpClient.GetAsync($"person/cedula/{identificationNumber}");
            if (!response.IsSuccessStatusCode && isPasssport == false)
                return new Response<IdentityPersonResponseDTO>($"No se pudo encontrar la persona", false);

            if (!response.IsSuccessStatusCode && isPasssport)
                return new Response<IdentityPersonResponseDTO>(null, "No encontrado");

            var responseContent = await response.Content.ReadAsStringAsync();
            var person = JsonConvert.DeserializeObject<IdentityPersonResponseDTO>(responseContent);
            return new Response<IdentityPersonResponseDTO>(person, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener persona");
            return new Response<IdentityPersonResponseDTO>($"Error al obtener la persona", false);
        }
    }

    public async Task<PagedResponse<IdentityPersonResponseDTO>> GetPersonsAsync(int? pageNumber, int? pageSize, string? searchTerm, EnumStatus? status)
    {
        if (pageNumber == null || pageNumber <= 0)
            pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100)
            pageSize = 50;

        try
        {
            var query = string.Join("&", new[]
            {
                pageNumber.HasValue ? $"pageNumber={pageNumber}" : null,
                pageSize.HasValue ? $"pageSize={pageSize}" : null,
                !string.IsNullOrWhiteSpace(searchTerm) ? $"search={Uri.EscapeDataString(searchTerm)}" : null,
                status.HasValue ? $"status={(int)status.Value}" : null
            }.Where(x => x != null));

            var url = string.IsNullOrEmpty(query) ? "person" : $"person?{query}";
            var response = await _httpClient.GetAsync(url);

            var headers = response.Headers;
            headers.TryGetValues("X-Total", out var totalCountValues);
            headers.TryGetValues("X-Page-Count", out var totalPagesValues);

            var totalCount = int.TryParse(totalCountValues?.FirstOrDefault(), out var count) ? count : 0;
            var totalPages = int.TryParse(totalPagesValues?.FirstOrDefault(), out var pages) ? pages : 0;


            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error al obtener personas. StatusCode: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                var errorMessage = string.IsNullOrWhiteSpace(errorContent)
                    ? $"Error HTTP {(int)response.StatusCode} - {response.StatusCode}"
                    : ExtractErrorMessage(errorContent);

                return new PagedResponse<IdentityPersonResponseDTO>(
                    new List<IdentityPersonResponseDTO>(),
                    $"Error al obtener las personas: {errorMessage}",
                    new List<string> { $"StatusCode: {response.StatusCode}" }
                );
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var persons = JsonConvert.DeserializeObject<List<IdentityPersonResponseDTO>>(responseContent) ?? new List<IdentityPersonResponseDTO>();
            return new PagedResponse<IdentityPersonResponseDTO>(
                persons,
                pageNumber ?? 1,
                pageSize ?? 50,
                totalCount,
                totalPages
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener personas");
            return new PagedResponse<IdentityPersonResponseDTO>(new List<IdentityPersonResponseDTO>(),
          "Error al listar las Personas"
      );
        }
    }
    public async Task<Response<IdentityPersonResponseDTO>> UpdatePersonAsync(Guid id, UpdatePersonDTO updatePersonDto)
    {
        try
        {
            var json = JsonConvert.SerializeObject(updatePersonDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"person/{id}", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = ExtractErrorMessage(errorContent);
                return new Response<IdentityPersonResponseDTO>($"Error al actualizar la persona: {errorMessage}", false);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var person = JsonConvert.DeserializeObject<IdentityPersonResponseDTO>(responseContent);
            return new Response<IdentityPersonResponseDTO>(person, "Persona actualizada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar persona");
            return new Response<IdentityPersonResponseDTO>("Error al actualizar persona", false);
        }
    }

    public async Task<Response<IdentityPersonResponseDTO>> UpdateProfilePhotoAsync(Guid id, Guid profilePhotoId)
    {
        try
        {
            var json = JsonConvert.SerializeObject(new { profilePhotoId });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"person/{id}/profile-photo", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = ExtractErrorMessage(errorContent);
                return new Response<IdentityPersonResponseDTO>($"Error al actualizar la foto de perfil: {errorMessage}", false);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var person = JsonConvert.DeserializeObject<IdentityPersonResponseDTO>(responseContent);
            return new Response<IdentityPersonResponseDTO>(person, "Foto de perfil actualizada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar la foto de perfil");
            return new Response<IdentityPersonResponseDTO>("Error al actualizar la foto de perfil", false);
        }
    }

    public async Task<Response<IdentityPersonResponseDTO>> UploadPersonPhotoAsync(Guid personId, IFormFile file)
    {
        if (!_isDmsConfigured)
            return new Response<IdentityPersonResponseDTO>(DmsUnavailableMessage, false);

        try
        {
            using var formContent = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            formContent.Add(streamContent, "file", file.FileName);
            formContent.Add(new StringContent("profile-picture"), "contextType");
            formContent.Add(new StringContent(personId.ToString()), "contextId");
            formContent.Add(new StringContent("profile-photo"), "documentTypeId");

            var dmsClient = await GetAuthenticatedDmsClientAsync();
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/internal/documents")
            {
                Content = formContent
            };
            request.Headers.Add("X-Environment-ID", _dmsEnvironmentId);

            var dmsResponse = await dmsClient.SendAsync(request);
            var dmsResponseContent = await dmsResponse.Content.ReadAsStringAsync();

            if (!dmsResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Error al subir la foto al DMS. StatusCode: {StatusCode}, Content: {Content}",
                    dmsResponse.StatusCode, dmsResponseContent);
                return new Response<IdentityPersonResponseDTO>(
                    $"Error al subir la foto al DMS: {ExtractErrorMessage(dmsResponseContent)}", false);
            }

            var document = JsonConvert.DeserializeObject<DmsDocumentUploadResponseDTO>(dmsResponseContent);
            if (document == null || document.DocumentId == Guid.Empty)
                return new Response<IdentityPersonResponseDTO>("El DMS no devolvió un documentId válido.", false);

            return await UpdateProfilePhotoAsync(personId, document.DocumentId);
        }
        catch (Exception ex) when (IsDmsUnavailable(ex))
        {
            _logger.LogWarning(ex, "DMS no disponible al subir la foto de perfil de la persona {PersonId}", personId);
            return new Response<IdentityPersonResponseDTO>(DmsUnavailableMessage, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al subir la foto de perfil de la persona {PersonId}", personId);
            return new Response<IdentityPersonResponseDTO>("Error al subir la foto de perfil", false);
        }
    }

    public async Task<Response<PersonPhotoDTO>> GetPersonPhotoAsync(Guid personId)
    {
        try
        {
            var personResult = await GetPersonByIdAsync(personId);
            if (!personResult.Succeeded || personResult.Data == null)
                return new Response<PersonPhotoDTO>("No se pudo encontrar la persona", false);

            var profilePhotoId = personResult.Data.ProfilePhotoId;
            if (profilePhotoId == null || profilePhotoId == Guid.Empty)
                return new Response<PersonPhotoDTO>("La persona no tiene una foto de perfil registrada", false);

            if (!_isDmsConfigured)
                return new Response<PersonPhotoDTO>(DmsUnavailableMessage, false);

            var dmsClient = await GetAuthenticatedDmsClientAsync();

            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/public/documents/{profilePhotoId}/content");
            request.Headers.Add("X-Environment-ID", _dmsEnvironmentId);
            AddActorHeaders(request.Headers, personId, "download");

            var dmsResponse = await dmsClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!dmsResponse.IsSuccessStatusCode)
            {
                var errorContent = await dmsResponse.Content.ReadAsStringAsync();
                _logger.LogWarning("Error al obtener la foto desde el DMS. StatusCode: {StatusCode}, Content: {Content}",
                    dmsResponse.StatusCode, errorContent);
                return new Response<PersonPhotoDTO>("No se pudo obtener la foto de perfil desde el DMS", false);
            }

            var bytes = await dmsResponse.Content.ReadAsByteArrayAsync();
            var contentType = dmsResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var fileName = dmsResponse.Content.Headers.ContentDisposition?.FileNameStar
                ?? dmsResponse.Content.Headers.ContentDisposition?.FileName?.Trim('"');

            var photo = new PersonPhotoDTO
            {
                Content = bytes,
                ContentType = contentType,
                FileName = fileName
            };

            return new Response<PersonPhotoDTO>(photo, "Ok");
        }
        catch (Exception ex) when (IsDmsUnavailable(ex))
        {
            _logger.LogWarning(ex, "DMS no disponible al obtener la foto de perfil de la persona {PersonId}", personId);
            return new Response<PersonPhotoDTO>(DmsUnavailableMessage, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la foto de perfil de la persona {PersonId}", personId);
            return new Response<PersonPhotoDTO>("Error al obtener la foto de perfil", false);
        }
    }

    public async Task<Response<List<IdentityPersonResponseDTO>>> GetPersonsByIdsAsync(IEnumerable<Guid> ids)
    {
        try
        {
            var idList = ids.ToList();
            if (idList.Count == 0)
                return new Response<List<IdentityPersonResponseDTO>>(new List<IdentityPersonResponseDTO>(), "OK");

            var json = JsonConvert.SerializeObject(idList);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("person/bulk", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error en bulk persons. StatusCode: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);
                return new Response<List<IdentityPersonResponseDTO>>(
                    $"Error al obtener personas en bulk: {ExtractErrorMessage(errorContent)}", false);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var persons = JsonConvert.DeserializeObject<List<IdentityPersonResponseDTO>>(responseContent)
                          ?? new List<IdentityPersonResponseDTO>();

            return new Response<List<IdentityPersonResponseDTO>>(persons, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener personas en bulk");
            return new Response<List<IdentityPersonResponseDTO>>("Error al obtener personas en bulk", false);
        }
    }

    public async Task<PagedResponse<IdentityPersonResponseDTO>> SearchPersonsAsync(
        int? pageNumber, int? pageSize, string searchTerm, IEnumerable<Guid>? ids = null)
    {
        if (pageNumber == null || pageNumber <= 0) pageNumber = 1;
        if (pageSize == null || pageSize <= 0 || pageSize > 100) pageSize = 50;

        try
        {
            var queryParts = new List<string>
            {
                $"pageNumber={pageNumber}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(searchTerm))
                queryParts.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");

            var idList = ids?.ToList() ?? new List<Guid>();
            var json = JsonConvert.SerializeObject(idList);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"person/search?{string.Join("&", queryParts)}", content);

            response.Headers.TryGetValues("X-Total", out var totalCountValues);
            response.Headers.TryGetValues("X-Page-Count", out var totalPagesValues);
            var totalCount = int.TryParse(totalCountValues?.FirstOrDefault(), out var tc) ? tc : 0;
            var totalPages = int.TryParse(totalPagesValues?.FirstOrDefault(), out var tp) ? tp : 0;

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error en search persons. StatusCode: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);
                return new PagedResponse<IdentityPersonResponseDTO>(
                    new List<IdentityPersonResponseDTO>(),
                    $"Error al buscar personas: {ExtractErrorMessage(errorContent)}",
                    new List<string> { $"StatusCode: {response.StatusCode}" });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var persons = JsonConvert.DeserializeObject<List<IdentityPersonResponseDTO>>(responseContent)
                          ?? new List<IdentityPersonResponseDTO>();

            return new PagedResponse<IdentityPersonResponseDTO>(
                persons, pageNumber.Value, pageSize.Value, totalCount, totalPages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar personas");
            return new PagedResponse<IdentityPersonResponseDTO>(
                new List<IdentityPersonResponseDTO>(), "Error al buscar personas");
        }
    }

    }
