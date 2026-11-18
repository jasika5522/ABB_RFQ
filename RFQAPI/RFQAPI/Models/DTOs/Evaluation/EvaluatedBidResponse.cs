namespace RFQAPI.Models.DTOs.Evaluation;

public class EvaluatedBidResponse
{
    public long BidId { get; set; }
    public long SupplierId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int RankPosition { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; }
}