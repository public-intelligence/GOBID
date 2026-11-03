using GOBID.Application.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GOBID.Application.DTOs.Personnel;

public record UpdatePersonDTO
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? IdentificationNumber { get; set; }
    public Guid? GenderId { get; set; }
    public string? Email { get; set; }
    public bool? EmailConfirmed { get; set; }
    public bool? IsExternal { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? Phone_NumberConfirmed { get; set; }

    public DateOnly? Birthdate { get; set; }

    public EnumStatus? Status { get; set; }
}
