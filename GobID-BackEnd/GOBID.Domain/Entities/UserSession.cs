namespace GOBID.Domain.Entities;

public class UserSession
{
    public Guid SessionId { get; set; }
    /// <summary>
    /// NULL cuando la sesión pertenece a una aplicación M2M (se usa AppCredentialId en su lugar).
    /// </summary>
    public Guid? UserId { get; set; }
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Referencia a la credencial M2M que originó esta sesión.
    /// NULL en sesiones de usuario humano.
    /// </summary>
    public Guid? AppCredentialId { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime AbsoluteExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }

    public virtual User? User { get; set; }
    public virtual ApplicationCredential? AppCredential { get; set; }
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
