namespace RFQAPI.Models.DTOs.Approval;

public class EvaluationDecisionResponse
{
    public string Status { get; set; } = string.Empty;
    public long RfqId { get; set; }
    public long RoundId { get; set; }
    public string? Decision { get; set; }
    public string? Reason { get; set; }
    public long? ApproverUserId { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public decimal? LowestTotalPrice { get; set; }
    public int? LowestRank { get; set; }
    public int? LowestRankSupplierCount { get; set; }
    public bool? IsLowestRankTied { get; set; }
}