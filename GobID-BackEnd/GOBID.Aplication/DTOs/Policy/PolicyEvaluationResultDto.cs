namespace GOBID.Application.DTOs.Policies;

public class PolicyEvaluationResultDto
{
    public bool IsAllowed { get; set; }
    public string Result { get; set; } = null!; // ALLOWED | DENIED
    public string? Reason { get; set; }
}
