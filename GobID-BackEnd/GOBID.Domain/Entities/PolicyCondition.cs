namespace GOBID.Domain.Entities;

public class PolicyCondition
{
    public int PolicyConditionId { get; set; }
    public Guid PolicyId { get; set; }
    public string ConditionKey { get; set; } = null!;
    public string ConditionValue { get; set; } = null!;

    public virtual Policy Policy { get; set; } = null!;
}
