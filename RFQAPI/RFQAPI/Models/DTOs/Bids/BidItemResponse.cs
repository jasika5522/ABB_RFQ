namespace RFQAPI.Models.DTOs.Bids;

public class BidItemResponse
{
    public long BidItemId { get; set; }
    public long RfqItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal RfqQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}