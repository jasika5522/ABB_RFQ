using RFQAPI.Helpers;
using RFQAPI.Models.DTOs.Evaluation;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Services.Implementations;

public class EvaluationService : IEvaluationService
{
    private readonly IEvaluationRepository _evaluationRepository;

    public EvaluationService(IEvaluationRepository evaluationRepository)
    {
        _evaluationRepository = evaluationRepository;
    }

    public async Task<EvaluateRoundResponse> EvaluateRoundAsync(
        long rfqId,
        long roundId,
        long userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0 || roundId <= 0)
        {
            throw new ApiException("Invalid RFQ or round ID.", StatusCodes.Status400BadRequest);
        }

        var result = await _evaluationRepository.EvaluateRoundAsync(
            rfqId, roundId, userId, ipAddress, cancellationToken);

        if (result is null)
        {
            throw new ApiException("RFQ or round not found.", StatusCodes.Status404NotFound);
        }

        return result;
    }

    public async Task<EvaluationResultsResponse> GetResultsAsync(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0 || roundId <= 0)
        {
            throw new ApiException("Invalid RFQ or round ID.", StatusCodes.Status400BadRequest);
        }

        var result = await _evaluationRepository.GetResultsAsync(rfqId, roundId, cancellationToken);
        if (result is null)
        {
            throw new ApiException("Evaluation results not found.", StatusCodes.Status404NotFound);
        }

        return result;
    }

    public async Task<EvaluationBidDetailResponse> GetBidDetailAsync(
        long rfqId,
        long roundId,
        long bidId,
        CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0 || roundId <= 0 || bidId <= 0)
        {
            throw new ApiException("Invalid RFQ, round, or bid ID.", StatusCodes.Status400BadRequest);
        }

        var result = await _evaluationRepository.GetBidDetailAsync(rfqId, roundId, bidId, cancellationToken);
        if (result is null)
        {
            throw new ApiException("Evaluation not found for the specified bid.", StatusCodes.Status404NotFound);
        }

        return result;
    }
}