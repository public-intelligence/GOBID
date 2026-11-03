using IdentityHub.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace IdentityHub.Domain.DTOs.Persons;

public record CreatePersonDTO
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    [MaxLength(11)]
    public required string IdentificationNumber { get; set; }
    public required EnumGender Gender { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public required DateOnly Birthdate { get; set; }
    public bool IsExternal { get; set; }
    public EnumStatus Status { get; set; }
    public Guid? ProfilePhotoId { get; set; }
}
