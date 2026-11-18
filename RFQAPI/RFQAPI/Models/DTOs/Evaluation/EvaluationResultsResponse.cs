namespace RFQAPI.Models.DTOs.Evaluation;

public class EvaluationResultsResponse
{
    public EvaluationSummaryDto Summary { get; set; } = null!;
    public IReadOnlyList<EvaluatedBidResponse> Bids { get; set; } = Array.Empty<EvaluatedBidResponse>();
}