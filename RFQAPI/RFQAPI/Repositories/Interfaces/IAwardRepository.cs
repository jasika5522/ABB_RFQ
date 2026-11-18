using RFQAPI.Models.DTOs.Awards;

namespace RFQAPI.Repositories.Interfaces;

public interface IAwardRepository
{
    Task<AwardResponse> CreateAsync(
        long rfqId,
        long roundId,
        long bidId,
        string awardComment,
        long awardedBy,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<AwardResponse?> GetByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AwardResponse> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? rfqNumber,
        string? companyName,
        CancellationToken cancellationToken = default);
}