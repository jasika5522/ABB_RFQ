namespace RFQAPI.Models.Entities;

public class Bid
{
    public long BidId { get; set; }
    public long RfqId { get; set; }
    public long RoundId { get; set; }
    public long SupplierId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}