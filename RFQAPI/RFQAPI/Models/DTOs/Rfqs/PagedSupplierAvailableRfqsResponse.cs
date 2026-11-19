namespace RFQAPI.Models.DTOs.Rfqs;

public class PagedSupplierAvailableRfqsResponse
{
    public IReadOnlyList<SupplierAvailableRfqSummaryResponse> Items { get; set; } =
        Array.Empty<SupplierAvailableRfqSummaryResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}