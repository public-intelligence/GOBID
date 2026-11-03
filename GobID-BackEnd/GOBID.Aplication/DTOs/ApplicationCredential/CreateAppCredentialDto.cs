namespace GOBID.Application.DTOs.ApplicationCredential;

public class CreateAppCredentialDto
{
    /// <summary>Descripción opcional para identificar la credencial (ej. "prod-2025", "ci-pipeline").</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Días de vigencia. Mínimo 1, máximo 365.
    /// Si no se especifica, se usa el valor configurado en AppSettings (default: 90).
    /// Usar 0 para que la credencial nunca expire.
    /// </summary>
    public int? ExpiryDays { get; set; }

    /// <summary>
    /// Contexto organización-aplicación bajo el cual se crea el rol M2M "RoleM2M".
    /// Requerido si se desean vincular claims (<see cref="ClaimIds"/>).
    /// </summary>
    public required int OrganizationApplicationId { get; set; }

    /// <summary>
    /// Ids de los claims a vincular al rol M2M que se creará automáticamente.
    /// </summary>
    public List<int> ClaimIds { get; set; } = [];
}

