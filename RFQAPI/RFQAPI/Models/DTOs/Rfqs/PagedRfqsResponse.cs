namespace RFQAPI.Models.DTOs.Rfqs;

public class PagedRfqsResponse
{
    public IReadOnlyList<RfqSummaryResponse> Items { get; set; } = Array.Empty<RfqSummaryResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}