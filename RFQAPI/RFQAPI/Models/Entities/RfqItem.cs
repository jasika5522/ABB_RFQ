namespace RFQAPI.Models.Entities;

public class RfqItem
{
    public long RfqItemId { get; set; }
    public long RfqId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}