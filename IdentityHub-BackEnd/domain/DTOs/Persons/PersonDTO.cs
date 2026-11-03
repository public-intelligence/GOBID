using IdentityHub.Domain.Enums;

namespace IdentityHub.Domain.DTOs.Persons;

public record PersonDTO
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string IdentificationNumber { get; init; } = null!;
    public string? Email { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool IsExternal { get; init; }
    public string? PhoneNumber { get; init; }
    public DateOnly Birthdate { get; init; }
    public bool PhoneNumberConfirmed { get; init; }
    public EnumStatus Status { get; init; }
    public EnumGender Gender { get; init; }
    public Guid? ProfilePhotoId { get; set; }
}
