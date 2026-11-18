using RFQAPI.Models.DTOs.Bids;
using RFQAPI.Models.Entities;

namespace RFQAPI.Repositories.Interfaces;

public interface IBidRepository
{
    Task<IReadOnlyList<long>> GetRfqItemIdsAsync(long rfqId, CancellationToken cancellationToken = default);
    Task<BidDetailData?> SubmitAsync(
        long rfqId,
        long roundId,
        long supplierId,
        long actingUserId,
        IReadOnlyList<SubmitBidItemRequest> items,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<BidDetailData?> WithdrawAsync(
        long bidId,
        long supplierId,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<BidSummary> Items, int TotalCount)> GetMyBidsAsync(
        long supplierId,
        int page,
        int pageSize,
        long? rfqId,
        string? status,
        CancellationToken cancellationToken = default);
    Task<BidDetailData?> GetDetailForSupplierAsync(
        long bidId,
        long supplierId,
        CancellationToken cancellationToken = default);
}

public sealed class BidDetailData
{
    public Bid Bid { get; init; } = null!;
    public string RfqNumber { get; init; } = string.Empty;
    public string RfqTitle { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public int RoundNumber { get; init; }
    public string RoundStatus { get; init; } = string.Empty;
    public DateTimeOffset RoundDeadline { get; init; }
    public IReadOnlyList<BidItemDetail> Items { get; init; } = Array.Empty<BidItemDetail>();
}