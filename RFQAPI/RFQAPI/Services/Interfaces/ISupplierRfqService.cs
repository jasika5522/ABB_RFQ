using RFQAPI.Models.DTOs.Rfqs;

namespace RFQAPI.Services.Interfaces;

public interface ISupplierRfqService
{
    Task<PagedSupplierAvailableRfqsResponse> GetAvailableAsync(
        int page,
        int pageSize,
        string? rfqNumber,
        string? title,
        string? search,
        CancellationToken cancellationToken = default);

    Task<SupplierAvailableRfqDetailResponse> GetAvailableDetailAsync(
        long rfqId,
        long supplierUserId,
        CancellationToken cancellationToken = default);
}