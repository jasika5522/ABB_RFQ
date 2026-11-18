using RFQAPI.Models.DTOs.Approval;

namespace RFQAPI.Repositories.Interfaces;

public interface IEvaluationDecisionRepository
{
    Task<EvaluationDecisionResponse> SubmitDecisionAsync(
        long rfqId,
        long roundId,
        long approverUserId,
        string decision,
        string? reason,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<EvaluationDecisionResponse?> GetDecisionAsync(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken = default);
}