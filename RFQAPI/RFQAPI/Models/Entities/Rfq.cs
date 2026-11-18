namespace RFQAPI.Models.Entities;

public class Rfq
{
    public long RfqId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long CreatedBy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}