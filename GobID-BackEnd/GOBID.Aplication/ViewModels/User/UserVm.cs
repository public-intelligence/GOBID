using GOBID.Application.DTOs.Personnel;
using GOBID.Application.DTOs.Pds;
using GOBID.Application.DTOs.UserApplication;
using GOBID.Application.ViewModels.UserApplication;

namespace GOBID.Application.ViewModels.User;

public class UserVm
{
    public Guid UserId { get; set; }

    public Guid PersonnelId { get; set; }

    public string? Email { get; set; }

    // G2: nunca salen en una respuesta (los endpoints que mapean desde UserDto los llenaban). Las API
    // serializan con Newtonsoft (Startup.AddNewtonsoftJson); el atributo de System.Text.Json cubre
    // cualquier otro serializador. Nadie deserializa UserVm desde una petición.
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserPassword { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string? PasswordResetToken { get; set; }


    public bool MustChangePassword { get; set; }

    public DateOnly? AdmissionDate { get; set; }

    public DateOnly? DischargeDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Position { get; set; }
    public string? RowVersion { get; set; }

    public bool IsActive { get; set; }
    public int? Count { get; set; }
    public UserApplicationUpsertDto[]? UserApplications { get; set; }
    public int? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public IdentityPersonResponseDTO? Personnel { get; set; }
    public List<int>? OrganizationIds { get; set; }
    public Guid? PersonPositionId { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid? PositionId { get; set; }
    public PdsLocalidadDto? Locality { get; set; }
    public PdsOrganizationalStructureDto? OrganizationalStructure { get; set; }
}