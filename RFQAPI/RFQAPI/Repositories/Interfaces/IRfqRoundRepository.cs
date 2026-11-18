using RFQAPI.Models.Entities;

namespace RFQAPI.Repositories.Interfaces;

public interface IRfqRoundRepository
{
    Task<RfqRound?> CreateAsync(
        long rfqId,
        DateTimeOffset deadline,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<RfqRound?> PublishAsync(
        long rfqId,
        long roundId,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<RfqRound?> CloseAsync(
        long rfqId,
        long roundId,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RfqRound>> GetByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default);
    Task<RfqRound?> GetByIdAsync(long rfqId, long roundId, CancellationToken cancellationToken = default);
}