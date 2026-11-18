namespace RFQAPI.Models.DTOs.Awards;

public class AwardResponse
{
    public long AwardId { get; set; }
    public long RfqId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public string RfqTitle { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public long RoundId { get; set; }
    public int RoundNumber { get; set; }
    public long BidId { get; set; }
    public long SupplierId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public decimal AwardedBidTotalPrice { get; set; }
    public long AwardedBy { get; set; }
    public string AwardComment { get; set; } = string.Empty;
    public DateTimeOffset AwardedAt { get; set; }
}