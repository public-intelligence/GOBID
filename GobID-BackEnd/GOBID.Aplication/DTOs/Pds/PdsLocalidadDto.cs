using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Pds;

public class PdsLocalidadDto
{
    [JsonProperty("id")] public Guid Id { get; set; }
    [JsonProperty("codigo")] public string Codigo { get; set; } = null!;
    [JsonProperty("nombreOficial")] public string NombreOficial { get; set; } = null!;
    [JsonProperty("tipoLocalidadId")] public Guid TipoLocalidadId { get; set; }
    [JsonProperty("tipoNombre")] public string? TipoNombre { get; set; }
    [JsonProperty("estadoLocalidadId")] public Guid EstadoLocalidadId { get; set; }
    [JsonProperty("estadoNombre")] public string? EstadoNombre { get; set; }
    [JsonProperty("estadoColor")] public string? EstadoColor { get; set; }
    [JsonProperty("provinciaId")] public Guid? ProvinciaId { get; set; }
    [JsonProperty("provinciaNombre")] public string? ProvinciaNombre { get; set; }
    [JsonProperty("municipioId")] public Guid? MunicipioId { get; set; }
    [JsonProperty("municipioNombre")] public string? MunicipioNombre { get; set; }
    [JsonProperty("latitud")] public double? Latitud { get; set; }
    [JsonProperty("longitud")] public double? Longitud { get; set; }
}

public class PdsLocalidadFilterDto
{
    public string? Nombre { get; set; }
    public string? Codigo { get; set; }
    public Guid? TipoId { get; set; }
    public Guid? EstadoId { get; set; }
    public Guid? NaturalezaId { get; set; }
    public Guid? RegionId { get; set; }
    public Guid? ProvinciaId { get; set; }
    public Guid? MunicipioId { get; set; }
    public Guid? UnidadId { get; set; }
    public Guid? ServicioId { get; set; }
    public Guid? InstrumentoLegalId { get; set; }
}
