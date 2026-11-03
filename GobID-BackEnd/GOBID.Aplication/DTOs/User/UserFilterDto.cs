namespace GOBID.Application.DTOs.User;

public record UserFilterDto
{
    public Guid? UserId{ get; set; }
    public Guid? PersonnnelId { get; set; }
    public string? EmailLike { get; set; }
    public bool? IsActive { get; set; }
}
