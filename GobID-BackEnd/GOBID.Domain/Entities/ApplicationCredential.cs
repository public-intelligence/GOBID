namespace GOBID.Domain.Entities;

public class ApplicationCredential
{
    public Guid AppCredentialId { get; set; }

    public Guid ApplicationId { get; set; }

    /// <summary>
    /// BCrypt hash (cost=12) del client_secret. El secreto en claro NUNCA se almacena.
    /// </summary>
    public string ClientSecretHash { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Fecha de expiración. Null = no expira nunca.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Se actualiza en cada autenticación exitosa (fire-and-forget, no bloqueante).
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid? RevokedBy { get; set; }

    /// <summary>
    /// Referencia a la credencial que reemplazó a esta durante una rotación.
    /// Permite trazar la cadena completa de rotación (igual que RefreshToken.ReplacedBy).
    /// </summary>
    public Guid? RotatedByCredentialId { get; set; }

    public virtual Application? Application { get; set; }

    public virtual ApplicationCredential? RotatedByCredential { get; set; }

    public virtual ICollection<UserSession> UserSessions { get; set; } = [];

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
