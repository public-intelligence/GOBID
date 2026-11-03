namespace GOBID.Domain.Entities;

/// <summary>
/// Asigna roles del sistema a una aplicación M2M dentro de un contexto de organización-aplicación.
/// Equivalente a UserRole, pero para service accounts (aplicaciones que se autentican via M2M).
/// </summary>
public class ApplicationRole
{
    public int ApplicationRoleId { get; set; }

    public Guid AppCredentialId { get; set; }

    public int OrganizationApplicationId { get; set; }

    public int RoleId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ApplicationCredential? ApplicationCredential { get; set; }

    public virtual OrganizationApplication? OrganizationApplication { get; set; }

    public virtual Role? Role { get; set; }

    public virtual ICollection<ApplicationRoleClaim> ApplicationRoleClaims { get; set; } = [];
}
