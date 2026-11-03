using GOBID.Application.DTOs.UserApplication;
using GOBID.Application.DTOs.UserRole;
using Newtonsoft.Json;

namespace GOBID.Application.DTOs.User;

public class UserUpsertDto
{
    public Guid? UserId { get; set; }

    public Guid PersonnelId { get; set; }

    public string? Email { get; set; }

    public string? UserPassword { get; set; }


    public bool MustChangePassword { get; set; }

    public DateOnly? AdmissionDate { get; set; }

    public DateOnly? DischargeDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    [JsonConverter(typeof(BoolToIntJsonConverter))]
    public bool IsActive { get; set; } 
    public bool IsExternal { get; set; }
    public string? Position { get; set; }
    public int OrganizationId { get; set; } 
    public Guid? PersonPositionId { get; set; }
    public Guid? PositionId { get; set; }
    public UserApplicationUpsertDto[]? UserApplications { get; set; }
}