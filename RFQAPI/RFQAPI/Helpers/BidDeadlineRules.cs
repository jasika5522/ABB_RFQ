namespace RFQAPI.Helpers;

/// <summary>
/// Bid-round deadline eligibility. Bidding is allowed only while the deadline is strictly in the future
/// (same semantics as PostgreSQL <c>@Deadline &gt; CURRENT_TIMESTAMP</c>).
/// </summary>
public static class BidDeadlineRules
{
    public static bool IsBiddingOpen(DateTimeOffset deadline, DateTimeOffset utcNow) => deadline > utcNow;
}