using RFQAPI.Helpers;
using Xunit;

namespace RFQAPI.Tests;

/// <summary>
/// Mandatory test 1: deadline validation (deterministic clock via injected instants).
/// </summary>
public class BidDeadlineRulesTests
{
    private static readonly DateTimeOffset ReferenceUtc = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsBiddingOpen_WhenDeadlineIsAfterNow_ReturnsTrue()
    {
        var deadline = ReferenceUtc.AddHours(1);
        Assert.True(BidDeadlineRules.IsBiddingOpen(deadline, ReferenceUtc));
    }

    [Fact]
    public void IsBiddingOpen_WhenDeadlineIsBeforeNow_ReturnsFalse()
    {
        var deadline = ReferenceUtc.AddMinutes(-1);
        Assert.False(BidDeadlineRules.IsBiddingOpen(deadline, ReferenceUtc));
    }

    [Fact]
    public void IsBiddingOpen_WhenDeadlineEqualsNow_ReturnsFalse_StrictFutureRule()
    {
        Assert.False(BidDeadlineRules.IsBiddingOpen(ReferenceUtc, ReferenceUtc));
    }
}