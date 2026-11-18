namespace RFQAPI.Models.DTOs.Evaluation;

public class EvaluationBidDetailResponse
{
    public long RfqId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public long RoundId { get; set; }
    public int RoundNumber { get; set; }
    public long BidId { get; set; }
    public long SupplierId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int RankPosition { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; }
    public IReadOnlyList<EvaluationLineItemResponse> Items { get; set; } = Array.Empty<EvaluationLineItemResponse>();
}