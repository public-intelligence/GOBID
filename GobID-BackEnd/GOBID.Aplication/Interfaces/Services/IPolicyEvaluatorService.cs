using GOBID.Application.DTOs.Policies;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IPolicyEvaluatorService
{
    Task<Response<PolicyEvaluationResultDto>> EvaluateAsync(Guid userApplicationId, string ipAddress, string? userAgent);
}
