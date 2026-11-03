using GOBID.Application.DTOs.EntityOrganization;
using GOBID.Application.DTOs.Personnel;
using GOBID.Application.Enums;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace GOBID.Infrastructure.Services;

public class LegalEntityService : ILegalEntityService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LegalEntityService> _logger;
    private readonly string _baseUrl;

    public LegalEntityService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LegalEntityService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Configurar desde appsettings
        _baseUrl = configuration["PersonApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException("PersonApiSettings:BaseUrl no está configurado");

        // Configurar HttpClient
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

    }

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

    public async Task<Response<LegalEntityDTO>> CreateOrganizationAsync(CreateLegalEntityDTO CreateEntityOrganization)
    {   
        try
        {
            var json = JsonConvert.SerializeObject(CreateEntityOrganization);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("organization", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = ExtractErrorMessage(errorContent);
                return new Response<LegalEntityDTO>(
                    $"Error al crear la organización: {errorMessage}",
                    false);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var organization = JsonConvert.DeserializeObject<LegalEntityDTO>(responseContent);
            return new Response<LegalEntityDTO>(organization, "Organización creada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear organización");
            return new Response<LegalEntityDTO>($"Error al crear la organización", false);
        }
    }

    public async Task<Response<LegalEntityDTO>> GetOrganizationByIdentificationNumberAsync(string identificationNumber)
    {
        try
        {
            var response = await _httpClient.GetAsync($"organization/rnc/{identificationNumber}");
            if (!response.IsSuccessStatusCode)
                return new Response<LegalEntityDTO>($"Error: {response.StatusCode}", false);

            var responseContent = await response.Content.ReadAsStringAsync();
            var organization = JsonConvert.DeserializeObject<LegalEntityDTO>(responseContent);
            return new Response<LegalEntityDTO>(organization, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener organización");
            return new Response<LegalEntityDTO>("Error al obtener organización", false);
        }
    }

    public async Task<Response<LegalEntityDTO>> GetOrganizationByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"organization/{id}");
            if (!response.IsSuccessStatusCode)
                return new Response<LegalEntityDTO>($"Error: {response.StatusCode}", false);

            var responseContent = await response.Content.ReadAsStringAsync();
            var organization = JsonConvert.DeserializeObject<LegalEntityDTO>(responseContent);
            return new Response<LegalEntityDTO>(organization, "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener organización");
            return new Response<LegalEntityDTO>("Error al obtener organización", false);
        }
    }

    public async Task<PagedResponse<LegalEntityDTO>> GetOrganizationsAsync(int? pageNumber, int? pageSize, string? searchTerm, EnumStatus? status)
    {
        try
        {
            var query = string.Join("&", new[]
            {
                pageNumber.HasValue ? $"pageNumber={pageNumber}" : null,
                pageSize.HasValue ? $"pageSize={pageSize}" : null,
                !string.IsNullOrWhiteSpace(searchTerm) ? $"search={Uri.EscapeDataString(searchTerm)}" : null,
                status.HasValue ? $"status={(int)status.Value}" : null
            }.Where(x => x != null));
            var url = string.IsNullOrEmpty(query) ? "organization" : $"organization?{query}";
            var response = await _httpClient.GetAsync(url);

            var headers = response.Headers;
            headers.TryGetValues("X-Total", out var totalCountValues);
            headers.TryGetValues("X-Page-Count", out var totalPagesValues);

            var totalCount = int.TryParse(totalCountValues?.FirstOrDefault(), out var count) ? count : 0;
            var totalPages = int.TryParse(totalPagesValues?.FirstOrDefault(), out var pages) ? pages : 0;


            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = ExtractErrorMessage(errorContent);
                return new PagedResponse<LegalEntityDTO>([],
                    $"Error al obtener las organizaciones: {errorMessage}"
                );

            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var organization = JsonConvert.DeserializeObject<List<LegalEntityDTO>>(responseContent) ?? new List<LegalEntityDTO>();
            return new PagedResponse<LegalEntityDTO>(
                organization,
                pageNumber ?? 1,
                pageSize ?? 10,
                totalCount,
                totalPages
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener personas");
            return new PagedResponse<LegalEntityDTO>(new List<LegalEntityDTO>(),
          "Error al listar las Personas"
      );
        }
    }
    public async Task<Response<LegalEntityDTO>> UpdateOrganizationAsync(Guid id, UpdateLegalEntityDTO updatePersonDto)
    {
        try
        {
            var json = JsonConvert.SerializeObject(updatePersonDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"organization/{id}", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = ExtractErrorMessage(errorContent);
                return new Response<LegalEntityDTO>($"Error al actualizar organización: {errorMessage}", false);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var organization = JsonConvert.DeserializeObject<LegalEntityDTO>(responseContent);
            return new Response<LegalEntityDTO>(organization, "Organización actualizada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar organización");
            return new Response<LegalEntityDTO>("Error al actualizar organización", false);
        }
    }
}
