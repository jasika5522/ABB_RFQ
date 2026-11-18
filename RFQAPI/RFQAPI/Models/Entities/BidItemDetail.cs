namespace RFQAPI.Models.Entities;

public class BidItemDetail : BidItem
{
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal RfqQuantity { get; set; }
}