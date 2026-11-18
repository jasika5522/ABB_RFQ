namespace RFQAPI.Models.DTOs.Evaluation;

public class EvaluationSummaryDto
{
    public long RfqId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public long RoundId { get; set; }
    public int RoundNumber { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTimeOffset? EvaluatedAt { get; set; }
    public decimal LowestTotalPrice { get; set; }
    public int LowestRank { get; set; }
    public bool IsLowestRankTied { get; set; }
    public int LowestRankSupplierCount { get; set; }
}