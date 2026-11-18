using RFQAPI.Models.DTOs.Bids;

namespace RFQAPI.Services.Interfaces;

public interface IBidService
{
    Task<BidResponse> SubmitAsync(
        long rfqId,
        long roundId,
        long userId,
        SubmitBidRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<BidResponse> WithdrawAsync(
        long bidId,
        long userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<PagedBidsResponse> GetMyBidsAsync(
        long userId,
        int page,
        int pageSize,
        long? rfqId,
        string? status,
        CancellationToken cancellationToken = default);
    Task<BidDetailResponse> GetMyBidDetailAsync(
        long bidId,
        long userId,
        CancellationToken cancellationToken = default);
}