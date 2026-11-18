using RFQAPI.Models.DTOs.Rfqs;
using RFQAPI.Models.Entities;

namespace RFQAPI.Repositories.Interfaces;

public interface IRfqRepository
{
    Task<bool> RfqNumberExistsAsync(string rfqNumber, CancellationToken cancellationToken = default);
    Task<Rfq?> GetByIdAsync(long rfqId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RfqItem>> GetItemsByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default);
    Task<RfqDetailResult?> CreateWithItemsAsync(
        CreateRfqRequest request,
        long createdBy,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<RfqDetailResult?> UpdateDraftWithItemsAsync(
        long rfqId,
        UpdateRfqRequest request,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Rfq> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? status,
        string? rfqNumber,
        string? title,
        CancellationToken cancellationToken = default);
}

public sealed class RfqDetailResult
{
    public Rfq Rfq { get; init; } = null!;
    public IReadOnlyList<RfqItem> Items { get; init; } = Array.Empty<RfqItem>();
}