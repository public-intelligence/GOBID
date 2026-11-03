namespace GOBID.Application.DTOs.ApplicationCredential;

/// <summary>
/// Representación pública de una credencial (sin secreto).
/// </summary>
public class AppCredentialDto
{
    public Guid AppCredentialId { get; set; }
    public Guid ApplicationId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? RotatedByCredentialId { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}

/// <summary>
/// Devuelto únicamente en la creación. Incluye el client_secret en claro UNA SOLA VEZ.
/// </summary>
public class AppCredentialCreatedDto : AppCredentialDto
{
    /// <summary>
    /// Secret en claro. Guárdalo inmediatamente — no se puede recuperar después.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Id del ApplicationRole M2M creado automáticamente. Null si no se solicitó contexto de organización.
    /// </summary>
    public int? ApplicationRoleId { get; set; }
}
