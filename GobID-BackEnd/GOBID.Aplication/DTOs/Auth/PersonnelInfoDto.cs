using System;

namespace GOBID.Application.DTOs.Auth;

public class PersonnelInfoDto
{
    public Guid? UserId { get; set; }
    public Guid PersonnelId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string IdentificationNumber { get; set; }
    public required string Position { get; set; }
    public required string OrganizationalStructure { get; set; }
    public required string OccupationalGroup { get; set; }
    public required string AreaType { get; set; }
    public required string AreaId { get; set; }
    public required string Email { get; set; }
    public required string TopAreaId { get; set; }
    public required string TopAreaName { get; set; }
}