namespace IdentityHub.Domain.DTOs.Persons;

public record UpdateProfilePhotoDTO
{
    public required Guid ProfilePhotoId { get; set; }
}
