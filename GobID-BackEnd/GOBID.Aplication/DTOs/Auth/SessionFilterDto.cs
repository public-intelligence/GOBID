namespace GOBID.Application.DTOs.Auth;

public class SessionFilterDto
{
    public bool? IsActive { get; set; }
    public Guid? ApplicationId { get; set; }
    public Guid? UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? IdentificationNumber { get; set; }
}
