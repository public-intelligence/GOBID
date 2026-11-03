using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Pds;

public class PdsPositionDto
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    [JsonProperty("competencies")]
    public string? Competencies { get; set; }

    [JsonProperty("grade")]
    public int Grade { get; set; }

    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("minSalary")]
    public decimal MinSalary { get; set; }

    [JsonProperty("maxSalary")]
    public decimal MaxSalary { get; set; }

    [JsonProperty("isManager")]
    public bool IsManager { get; set; }

    [JsonProperty("occupationalGroup")]
    public PdsOccupationalGroupDto? OccupationalGroup { get; set; }

    [JsonProperty("organizationalStructure")]
    public PdsOrganizationalStructureDto? OrganizationalStructure { get; set; }
}

public class PdsOccupationalGroupDto
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = null!;
}

public class PdsOrganizationalStructureDto
{
    [JsonProperty("id")]
    public Guid Id { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; } = null!;

    [JsonProperty("name")]
    public string Name { get; set; } = null!;
}
