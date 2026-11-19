using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Rfqs;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Services.Implementations;

public class SupplierRfqService : ISupplierRfqService
{
    private const int MaxPageSize = 100;

    private readonly ISupplierRfqRepository _supplierRfqRepository;
    private readonly IRfqRepository _rfqRepository;
    private readonly ISupplierRepository _supplierRepository;

    public SupplierRfqService(
        ISupplierRfqRepository supplierRfqRepository,
        IRfqRepository rfqRepository,
        ISupplierRepository supplierRepository)
    {
        _supplierRfqRepository = supplierRfqRepository;
        _rfqRepository = rfqRepository;
        _supplierRepository = supplierRepository;
    }

    public async Task<PagedSupplierAvailableRfqsResponse> GetAvailableAsync(
        int page,
        int pageSize,
        string? rfqNumber,
        string? title,
        string? search,
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

        var (items, totalCount) = await _supplierRfqRepository.GetAvailableAsync(
            page, pageSize, rfqNumber, title, search, cancellationToken);

        return new PagedSupplierAvailableRfqsResponse
        {
            Items = items.Select(SupplierRfqMapper.ToSummary).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<SupplierAvailableRfqDetailResponse> GetAvailableDetailAsync(
        long rfqId,
        long supplierUserId,
        CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0)
        {
            throw new ApiException("Invalid RFQ ID.", StatusCodes.Status400BadRequest);
        }

        var row = await _supplierRfqRepository.GetAvailableDetailAsync(rfqId, cancellationToken);
        if (row is null)
        {
            throw new ApiException("RFQ not found or not available for bidding.", StatusCodes.Status404NotFound);
        }

        var items = await _rfqRepository.GetItemsByRfqIdAsync(rfqId, cancellationToken);
        var supplier = await _supplierRepository.GetByUserIdAsync(supplierUserId, cancellationToken);
        if (supplier is null)
        {
            throw new ApiException("Supplier profile not found.", StatusCodes.Status404NotFound);
        }

        var eligibility = BuildEligibility(supplier.ApprovalStatus, row.Deadline);

        return SupplierRfqMapper.ToDetail(row, items, eligibility);
    }

    private static SupplierRfqEligibilityResponse BuildEligibility(string approvalStatus, DateTimeOffset deadline)
    {
        var deadlineOpen = deadline > DateTimeOffset.UtcNow;

        if (!string.Equals(approvalStatus, SupplierApprovalStatuses.Approved, StringComparison.Ordinal))
        {
            var message = approvalStatus switch
            {
                SupplierApprovalStatuses.Pending =>
                    "Your supplier account is pending approval. You may review this RFQ, but bidding is unavailable until approved.",
                SupplierApprovalStatuses.Suspended =>
                    "Your supplier account is suspended. Bidding is unavailable.",
                _ => "Bidding is unavailable for your supplier account."
            };

            return new SupplierRfqEligibilityResponse
            {
                ApprovalStatus = approvalStatus,
                CanSubmitBid = false,
                Message = message
            };
        }

        if (!deadlineOpen)
        {
            return new SupplierRfqEligibilityResponse
            {
                ApprovalStatus = approvalStatus,
                CanSubmitBid = false,
                Message = "The submission deadline has passed. Bidding is no longer available."
            };
        }

        return new SupplierRfqEligibilityResponse
        {
            ApprovalStatus = approvalStatus,
            CanSubmitBid = true,
            Message = null
        };
    }
}