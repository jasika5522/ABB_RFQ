using RFQAPI.Models.DTOs.Rfqs;

namespace RFQAPI.Services.Interfaces;

public interface IRfqService
{
    Task<RfqDetailResponse> CreateAsync(
        CreateRfqRequest request,
        long createdBy,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<RfqDetailResponse> UpdateDraftAsync(
        long rfqId,
        UpdateRfqRequest request,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<RfqDetailResponse> GetByIdAsync(long rfqId, CancellationToken cancellationToken = default);
    Task<PagedRfqsResponse> GetAllAsync(
        int page,
        int pageSize,
        string? status,
        string? rfqNumber,
        string? title,
        CancellationToken cancellationToken = default);
}