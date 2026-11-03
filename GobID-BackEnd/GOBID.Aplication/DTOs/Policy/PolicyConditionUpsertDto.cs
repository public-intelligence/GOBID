using Newtonsoft.Json;

namespace GOBID.Application.DTOs.Policies;

public class PolicyConditionUpsertDto
{
    public string ConditionKey { get; set; } = null!;

    public string ConditionValue { get; set; } = null!;
}
