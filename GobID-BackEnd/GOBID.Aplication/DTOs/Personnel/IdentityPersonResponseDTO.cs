using GOBID.Application.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GOBID.Application.DTOs.Personnel;

public class IdentityPersonResponseDTO
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; } 
    public required string LastName { get; set; } 
    public required string IdentificationNumber { get; set; } 

    [JsonConverter(typeof(StringEnumConverter))]
    public EnumGender Gender { get; set; }

    public required DateOnly Birthdate { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsExternal { get; set; }
    public Guid? ProfilePhotoId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public EnumStatus Status { get; set; }
}
