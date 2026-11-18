namespace RFQAPI.Models.DTOs.Bids;

public class BidResponse
{
    public long BidId { get; set; }
    public long RfqId { get; set; }
    public long RoundId { get; set; }
    public long SupplierId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }
    public string? CurrencyCode { get; set; }
    public IReadOnlyList<BidItemResponse> Items { get; set; } = Array.Empty<BidItemResponse>();
}