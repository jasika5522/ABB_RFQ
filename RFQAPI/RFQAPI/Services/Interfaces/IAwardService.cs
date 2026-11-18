using RFQAPI.Models.DTOs.Awards;

namespace RFQAPI.Services.Interfaces;

public interface IAwardService
{
    Task<AwardResponse> CreateAsync(
        long rfqId,
        long roundId,
        CreateAwardRequest request,
        long awardedBy,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<AwardResponse> GetByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default);
    Task<PagedAwardsResponse> GetAllAsync(
        int page,
        int pageSize,
        string? rfqNumber,
        string? companyName,
        CancellationToken cancellationToken = default);
}