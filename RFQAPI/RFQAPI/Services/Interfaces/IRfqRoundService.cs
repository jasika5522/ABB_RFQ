using RFQAPI.Models.DTOs.Rounds;

namespace RFQAPI.Services.Interfaces;

public interface IRfqRoundService
{
    Task<RfqRoundResponse> CreateAsync(
        long rfqId,
        CreateRfqRoundRequest request,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<RfqRoundResponse> PublishAsync(
        long rfqId,
        long roundId,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<RfqRoundResponse> CloseAsync(
        long rfqId,
        long roundId,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RfqRoundResponse>> GetByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default);
    Task<RfqRoundResponse> GetByIdAsync(long rfqId, long roundId, CancellationToken cancellationToken = default);
}