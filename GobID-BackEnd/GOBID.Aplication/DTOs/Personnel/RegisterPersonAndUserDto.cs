using GOBID.Application.Enums;
using System.ComponentModel.DataAnnotations;

namespace GOBID.Application.DTOs.Personnel;

public class RegisterPersonAndUserDto
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

    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
}
