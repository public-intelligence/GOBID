using IdentityHub.Domain.Enums;

namespace IdentityHub.Domain.DTOs.Persons;

public record UpdatePersonDTO
{
    public string? FirstName { get; set; } 
    public string? LastName { get; set; }
    public string? IdentificationNumber { get; set; } 
    public EnumGender? Gender { get; set; } 
    public string? Email { get; set; }
    public bool? EmailConfirmed { get; set; }
    public bool? IsExternal { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? Birthdate { get; set; }
    public bool? PhoneNumberConfirmed { get; set; }
    public Guid? ProfilePhotoId { get; set; }
    public EnumStatus? Status { get; init; }

}
