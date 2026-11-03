namespace GOBID.Application.DTOs.Policies;

public class PolicyEvaluationRequestDto
{
    public Guid UserApplicationId { get; set; }
    public string IpAddress { get; set; } = null!;
    public string? UserAgent { get; set; }
}
