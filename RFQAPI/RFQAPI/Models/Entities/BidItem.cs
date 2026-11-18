namespace RFQAPI.Models.Entities;

public class BidItem
{
    public long BidItemId { get; set; }
    public long BidId { get; set; }
    public long RfqId { get; set; }
    public long RfqItemId { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}