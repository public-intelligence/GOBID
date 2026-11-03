using IdentityHub.Domain.Enums;

namespace IdentityHub.Domain.Models;

public class Person
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; } = null!;
    public required string LastName { get; set; } = null!;
    public required string IdentificationNumber { get; set; } = null!;
    public required DateOnly Birthdate { get; set; } 
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsExternal { get; set; }
    public string? PhoneNumber { get; set; } 
    public bool PhoneNumberConfirmed { get; set; }
    public EnumStatus Status { get; set; }
    public EnumGender Gender { get; set; }
    public Guid? ProfilePhotoId { get; set; }

}
