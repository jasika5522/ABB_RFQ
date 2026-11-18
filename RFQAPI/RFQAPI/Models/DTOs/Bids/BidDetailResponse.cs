namespace RFQAPI.Models.DTOs.Bids;

public class BidDetailResponse
{
    public long BidId { get; set; }
    public long RfqId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public string RfqTitle { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public long RoundId { get; set; }
    public int RoundNumber { get; set; }
    public string RoundStatus { get; set; } = string.Empty;
    public DateTimeOffset RoundDeadline { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }
    public IReadOnlyList<BidItemResponse> Items { get; set; } = Array.Empty<BidItemResponse>();
}