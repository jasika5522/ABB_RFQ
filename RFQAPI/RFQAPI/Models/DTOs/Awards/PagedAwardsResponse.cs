namespace RFQAPI.Models.DTOs.Awards;

public class PagedAwardsResponse
{
    public IReadOnlyList<AwardResponse> Items { get; set; } = Array.Empty<AwardResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}