using RFQAPI.Models.DTOs.Approval;

namespace RFQAPI.Services.Interfaces;

public interface IEvaluationDecisionService
{
    Task<EvaluationDecisionResponse> SubmitDecisionAsync(
        long rfqId,
        long roundId,
        long approverUserId,
        EvaluationDecisionRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<EvaluationDecisionResponse> GetDecisionAsync(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken = default);
}