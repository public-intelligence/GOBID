namespace GOBID.Domain.Entities;

public class PolicyEvaluationLog
{
    public Guid PolicyEvaluationLogId { get; set; }
    public Guid UserId { get; set; }
    public Guid PolicyId { get; set; }
    public int OrganizationId { get; set; }
    public string EvaluationResult { get; set; } = null!; // ALLOWED | DENIED
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime EvaluatedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Policy Policy { get; set; } = null!;
    public virtual Organization Organization { get; set; } = null!;
}
