using GOBID.Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GOBID.Application.DTOs.EntityOrganization;

public record CreateLegalEntityDTO
{
    public required string LegalName { get; init; }
    public required string SocialName { get; init; }
    public required string IdentificationNumber { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public required string Type { get; init; }
    public string? StatusDGII { get; init; } 
    public EnumStatus Status { get; init; }
}
