namespace RFQAPI.Models.Entities;

public class Award
{
    public long AwardId { get; set; }
    public long RfqId { get; set; }
    public long RoundId { get; set; }
    public long BidId { get; set; }
    public long AwardedBy { get; set; }
    public string AwardComment { get; set; } = string.Empty;
    public DateTimeOffset AwardedAt { get; set; }
}