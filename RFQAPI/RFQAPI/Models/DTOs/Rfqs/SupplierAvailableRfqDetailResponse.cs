namespace RFQAPI.Models.DTOs.Rfqs;

public class SupplierAvailableRfqDetailResponse
{
    public long RfqId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public long PublishedRoundId { get; set; }
    public int RoundNumber { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset Deadline { get; set; }
    public IReadOnlyList<RfqItemResponse> Items { get; set; } = Array.Empty<RfqItemResponse>();
    public SupplierRfqEligibilityResponse Eligibility { get; set; } = null!;
}