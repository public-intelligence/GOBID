using System;

namespace GOBID.Domain.Entities;

public class RefreshToken
{
    public Guid TokenId { get; set; }

    public string? TokenHash { get; set; }

    /// <summary>
    /// NULL cuando el token pertenece a una sesión M2M (se usa AppCredentialId en su lugar).
    /// </summary>
    public Guid? UserId { get; set; }

    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Referencia a la credencial M2M utilizada para emitir este token.
    /// NULL en sesiones de usuario humano.
    /// </summary>
    public Guid? AppCredentialId { get; set; }

    public string? AuthData { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? ReplacedBy { get; set; }

    public string? CorrelationId { get; set; }

    public Guid? SessionId { get; set; }

    public virtual User? User { get; set; }

    public virtual ApplicationCredential? AppCredential { get; set; }

    public virtual UserSession? Session { get; set; }
}
