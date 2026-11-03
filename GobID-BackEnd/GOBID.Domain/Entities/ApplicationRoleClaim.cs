namespace GOBID.Domain.Entities;

/// <summary>
/// Vincula un rol de aplicación M2M (<see cref="ApplicationRole"/>) con un claim específico.
/// Permite definir qué claims tiene disponibles cada rol de una aplicación M2M.
/// </summary>
public class ApplicationRoleClaim
{
    public int ApplicationRoleClaimId { get; set; }

    public int ApplicationRoleId { get; set; }

    public int ClaimId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public virtual ApplicationRole? ApplicationRole { get; set; }

    public virtual Claim? Claim { get; set; }
}
