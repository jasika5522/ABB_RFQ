using RFQAPI.Models.DTOs.Evaluation;

namespace RFQAPI.Repositories.Interfaces;

public interface IEvaluationRepository
{
    Task<EvaluateRoundResponse?> EvaluateRoundAsync(
        long rfqId,
        long roundId,
        long evaluatedBy,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<EvaluationResultsResponse?> GetResultsAsync(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken = default);
    Task<EvaluationBidDetailResponse?> GetBidDetailAsync(
        long rfqId,
        long roundId,
        long bidId,
        CancellationToken cancellationToken = default);
}