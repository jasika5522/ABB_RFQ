using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Bids;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Services.Implementations;

public class BidService : IBidService
{
    private const int MaxPageSize = 100;

    private static readonly HashSet<string> ValidBidStatuses =
    [
        BidStatuses.Submitted,
        BidStatuses.Withdrawn,
        BidStatuses.Accepted,
        BidStatuses.Rejected
    ];

    private readonly IBidRepository _bidRepository;
    private readonly ISupplierRepository _supplierRepository;

    public BidService(IBidRepository bidRepository, ISupplierRepository supplierRepository)
    {
        _bidRepository = bidRepository;
        _supplierRepository = supplierRepository;
    }

    public async Task<BidResponse> SubmitAsync(
        long rfqId,
        long roundId,
        long userId,
        SubmitBidRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0 || roundId <= 0)
        {
            throw new ApiException("Invalid RFQ or round ID.", StatusCodes.Status400BadRequest);
        }

        var supplier = await _supplierRepository.GetByUserIdAsync(userId, cancellationToken);
        if (supplier is null)
        {
            throw new ApiException("Supplier profile not found.", StatusCodes.Status404NotFound);
        }

        var requiredItemIds = await _bidRepository.GetRfqItemIdsAsync(rfqId, cancellationToken);
        if (requiredItemIds.Count == 0)
        {
            throw new ApiException("RFQ not found.", StatusCodes.Status404NotFound);
        }

        var normalizedItems = ValidateAndNormalizeItems(request.Items, requiredItemIds);

        var result = await _bidRepository.SubmitAsync(
            rfqId,
            roundId,
            supplier.SupplierId,
            userId,
            normalizedItems,
            ipAddress,
            cancellationToken);

        if (result is null)
        {
            throw new ApiException("RFQ or round not found.", StatusCodes.Status404NotFound);
        }

        return BidMapper.ToResponse(result.Bid, result.CurrencyCode, result.Items);
    }

    public async Task<BidResponse> WithdrawAsync(
        long bidId,
        long userId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (bidId <= 0)
        {
            throw new ApiException("Invalid bid ID.", StatusCodes.Status400BadRequest);
        }

        var supplier = await _supplierRepository.GetByUserIdAsync(userId, cancellationToken);
        if (supplier is null)
        {
            throw new ApiException("Supplier profile not found.", StatusCodes.Status404NotFound);
        }

        var result = await _bidRepository.WithdrawAsync(
            bidId, supplier.SupplierId, userId, ipAddress, cancellationToken);

        if (result is null)
        {
            throw new ApiException("Bid not found.", StatusCodes.Status404NotFound);
        }

        return BidMapper.ToResponse(result.Bid, result.CurrencyCode, result.Items);
    }

    public async Task<PagedBidsResponse> GetMyBidsAsync(
        long userId,
        int page,
        int pageSize,
        long? rfqId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ApiException("Page must be at least 1.", StatusCodes.Status400BadRequest);
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ApiException($"Page size must be between 1 and {MaxPageSize}.", StatusCodes.Status400BadRequest);
        }

        if (!string.IsNullOrWhiteSpace(status) && !ValidBidStatuses.Contains(status))
        {
            throw new ApiException("Invalid bid status filter.", StatusCodes.Status400BadRequest);
        }

        var supplier = await _supplierRepository.GetByUserIdAsync(userId, cancellationToken);
        if (supplier is null)
        {
            throw new ApiException("Supplier profile not found.", StatusCodes.Status404NotFound);
        }

        var (items, totalCount) = await _bidRepository.GetMyBidsAsync(
            supplier.SupplierId, page, pageSize, rfqId, status, cancellationToken);

        return new PagedBidsResponse
        {
            Items = items.Select(BidMapper.ToSummary).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BidDetailResponse> GetMyBidDetailAsync(
        long bidId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        if (bidId <= 0)
        {
            throw new ApiException("Invalid bid ID.", StatusCodes.Status400BadRequest);
        }

        var supplier = await _supplierRepository.GetByUserIdAsync(userId, cancellationToken);
        if (supplier is null)
        {
            throw new ApiException("Supplier profile not found.", StatusCodes.Status404NotFound);
        }

        var result = await _bidRepository.GetDetailForSupplierAsync(bidId, supplier.SupplierId, cancellationToken);
        if (result is null)
        {
            throw new ApiException("Bid not found.", StatusCodes.Status404NotFound);
        }

        return new BidDetailResponse
        {
            BidId = result.Bid.BidId,
            RfqId = result.Bid.RfqId,
            RfqNumber = result.RfqNumber,
            RfqTitle = result.RfqTitle,
            CurrencyCode = result.CurrencyCode,
            RoundId = result.Bid.RoundId,
            RoundNumber = result.RoundNumber,
            RoundStatus = result.RoundStatus,
            RoundDeadline = result.RoundDeadline,
            Status = result.Bid.Status,
            SubmittedAt = result.Bid.SubmittedAt,
            WithdrawnAt = result.Bid.WithdrawnAt,
            Items = result.Items.Select(BidMapper.ToItemResponse).ToList()
        };
    }

    private static IReadOnlyList<SubmitBidItemRequest> ValidateAndNormalizeItems(
        IReadOnlyList<SubmitBidItemRequest> items,
        IReadOnlyList<long> requiredItemIds)
    {
        if (items.Count == 0)
        {
            throw new ApiException("At least one bid item is required.", StatusCodes.Status400BadRequest);
        }

        var requiredSet = requiredItemIds.ToHashSet();
        var seen = new HashSet<long>();

        foreach (var item in items)
        {
            if (item.UnitPrice <= 0)
            {
                throw new ApiException("Unit price must be greater than zero.", StatusCodes.Status400BadRequest);
            }

            if (!requiredSet.Contains(item.RfqItemId))
            {
                throw new ApiException(
                    $"RFQ item {item.RfqItemId} is invalid for this RFQ.",
                    StatusCodes.Status400BadRequest);
            }

            if (!seen.Add(item.RfqItemId))
            {
                throw new ApiException("Duplicate RFQ item IDs are not allowed.", StatusCodes.Status400BadRequest);
            }
        }

        if (seen.Count != requiredSet.Count)
        {
            throw new ApiException("All RFQ items must be quoted.", StatusCodes.Status400BadRequest);
        }

        return items;
    }
}