using GOBID.Application.DTOs.Personnel;
using Newtonsoft.Json;

namespace GOBID.Application.DTOs.User;

public class UserDto
{
    [JsonProperty("user_id")]
    public Guid? UserId { get; set; }

    [JsonProperty("personnel_id")]
    public Guid? PersonnelId { get; set; }

    public required string Email { get; set; }

    [JsonProperty("user_password")]
    public required string UserPassword { get; set; }

    [JsonProperty("security_key")]
    public required string SecurityKey { get; set; }

    [JsonProperty("password_reset_token")]
    public required string PasswordResetToken { get; set; }

    [JsonProperty("is_server_admin")]
    public bool IsServerAdmin { get; set; }
    [JsonProperty("is_domain_authenticated")]
    public bool IsDomainAuthenticated { get; set; }
    [JsonProperty("must_change_password")]
    public bool MustChangePassword { get; set; }
    [JsonProperty("admission_date")]
    public DateOnly? AdmissionDate { get; set; }
    [JsonProperty("discharge_date")]
    public DateOnly? DischargeDate { get; set; }
    [JsonProperty("created_at")]
    public DateTime? CreatedAt { get; set; }
    [JsonProperty("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    [JsonProperty("updated_by")]
    public Guid? UpdatedBy { get; set; }
    [JsonProperty("created_by")]
    public Guid? CreatedBy { get; set; }
    [JsonProperty("row_version")]
    public string? RowVersion { get; set; }
    [JsonProperty("is_active")]
    public bool IsActive { get; set; }
    public string? Position { get; set; }
    
    [JsonProperty("organization_name")]
    public string? OrganizationName { get; set; }
    [JsonProperty("person_position_id")]
    public Guid? PersonPositionId { get; set; }
    [JsonProperty("phone_number")]
    public string? PhoneNumber { get; set; }

    [JsonProperty("position_id")]
    public Guid? PositionId { get; set; }
}
