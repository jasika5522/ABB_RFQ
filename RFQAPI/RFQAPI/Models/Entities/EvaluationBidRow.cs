namespace RFQAPI.Models.Entities;

public class EvaluationBidRow
{
    public long BidId { get; set; }
    public long SupplierId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public int RankPosition { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; }
}