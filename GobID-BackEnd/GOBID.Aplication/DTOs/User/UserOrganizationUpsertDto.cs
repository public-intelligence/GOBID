namespace GOBID.Application.DTOs.User;

public class UserOrganizationUpsertDto
{
    public required int OrganizationId { get; set; }
    public required Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string Position { get; set; }
}
