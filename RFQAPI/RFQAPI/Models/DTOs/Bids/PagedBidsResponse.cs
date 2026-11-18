namespace RFQAPI.Models.DTOs.Bids;

public class PagedBidsResponse
{
    public IReadOnlyList<BidSummaryResponse> Items { get; set; } = Array.Empty<BidSummaryResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}