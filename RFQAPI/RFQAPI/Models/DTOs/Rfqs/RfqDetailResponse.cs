namespace RFQAPI.Models.DTOs.Rfqs;

public class RfqDetailResponse
{
    public long RfqId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public IReadOnlyList<RfqItemResponse> Items { get; set; } = Array.Empty<RfqItemResponse>();
}