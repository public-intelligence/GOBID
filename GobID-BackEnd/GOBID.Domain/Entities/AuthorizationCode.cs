using System;

namespace GOBID.Domain.Entities;

public class AuthorizationCode
{
    public Guid CodeId { get; set; }

    public string? CodeHash { get; set; }

    public Guid UserId { get; set; }

    public Guid ApplicationId { get; set; }

    public string? RedirectUri { get; set; }

    public string? AuthData { get; set; }

    public bool IsUsed { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
