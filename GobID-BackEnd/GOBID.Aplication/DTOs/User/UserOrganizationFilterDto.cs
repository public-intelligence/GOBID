namespace GOBID.Application.DTOs.User;

public class UserOrganizationFilterDto
{
    public string? Email { get; set; }
    public string? SearchTerm { get; set; }
    public int? RoleId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? OrganizationalStructureId { get; set; }

    /// <summary>
    /// Con <see cref="OrganizationalStructureId"/>: true incluye a las personas de las subunidades
    /// (departamentos, divisiones…). Por defecto false, solo la unidad indicada.
    /// </summary>
    public bool IncludeDescendants { get; set; }
}
