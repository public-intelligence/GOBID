namespace GOBID.Application.DTOs.Personnel;

public class PersonPhotoDTO
{
    public required byte[] Content { get; set; }
    public required string ContentType { get; set; }
    public string? FileName { get; set; }
}
