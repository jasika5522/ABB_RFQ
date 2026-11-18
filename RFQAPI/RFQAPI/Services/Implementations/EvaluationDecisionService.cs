using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Approval;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Services.Implementations;

public class EvaluationDecisionService : IEvaluationDecisionService
{
    private readonly IEvaluationDecisionRepository _decisionRepository;

    public EvaluationDecisionService(IEvaluationDecisionRepository decisionRepository)
    {
        _decisionRepository = decisionRepository;
    }

    public async Task<EvaluationDecisionResponse> SubmitDecisionAsync(
        long rfqId,
        long roundId,
        long approverUserId,
        EvaluationDecisionRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0 || roundId <= 0)
        {
            throw new ApiException("Invalid RFQ or round ID.", StatusCodes.Status400BadRequest);
        }

        var decision = request.Decision.Trim();
        if (!string.Equals(decision, EvaluationDecisions.Approved, StringComparison.Ordinal) &&
            !string.Equals(decision, EvaluationDecisions.Rejected, StringComparison.Ordinal))
        {
            throw new ApiException("Decision must be Approved or Rejected.", StatusCodes.Status400BadRequest);
        }

        return await _decisionRepository.SubmitDecisionAsync(
            rfqId,
            roundId,
            approverUserId,
            decision,
            request.Reason,
            ipAddress,
            cancellationToken);
    }

    public async Task<EvaluationDecisionResponse> GetDecisionAsync(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0 || roundId <= 0)
        {
            throw new ApiException("Invalid RFQ or round ID.", StatusCodes.Status400BadRequest);
        }

        var result = await _decisionRepository.GetDecisionAsync(rfqId, roundId, cancellationToken);
        if (result is null)
        {
            throw new ApiException("RFQ or round not found.", StatusCodes.Status404NotFound);
        }

        return result;
    }
}