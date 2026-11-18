using RFQAPI.Models.DTOs.Evaluation;

namespace RFQAPI.Services.Interfaces;

public interface IEvaluationService
{
    Task<EvaluateRoundResponse> EvaluateRoundAsync(
        long rfqId,
        long roundId,
        long userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<EvaluationResultsResponse> GetResultsAsync(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken = default);
    Task<EvaluationBidDetailResponse> GetBidDetailAsync(
        long rfqId,
        long roundId,
        long bidId,
        CancellationToken cancellationToken = default);
}