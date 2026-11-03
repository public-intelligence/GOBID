namespace GOBID.Application.DTOs.Personnel;

public class PersonnelDto
{
    public Guid PersonnelId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string IdentificationNumber { get; set; }
    public required string Position { get; set; }
}