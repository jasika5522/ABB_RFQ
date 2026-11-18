namespace RFQAPI.Models.DTOs.Bids;

public class BidSummaryResponse
{
    public long BidId { get; set; }
    public long RfqId { get; set; }
    public long RoundId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public string RfqTitle { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }
}