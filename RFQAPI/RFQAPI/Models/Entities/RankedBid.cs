namespace RFQAPI.Models.Entities;

public class RankedBid
{
    public long BidId { get; set; }
    public long SupplierId { get; set; }
    public decimal TotalPrice { get; set; }
    public long RankPosition { get; set; }
}