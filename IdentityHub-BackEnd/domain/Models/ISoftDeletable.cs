namespace IdentityHub.Domain.Models;

public interface ISoftDeletable
{
    public DateTime? DeletedAt { get; set; }
}
