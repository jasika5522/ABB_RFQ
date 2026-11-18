using RFQAPI.Helpers;
using RFQAPI.Models.DTOs.Rounds;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Services.Implementations;

public class RfqRoundService : IRfqRoundService
{
    private readonly IRfqRepository _rfqRepository;
    private readonly IRfqRoundRepository _rfqRoundRepository;

    public RfqRoundService(IRfqRepository rfqRepository, IRfqRoundRepository rfqRoundRepository)
    {
        _rfqRepository = rfqRepository;
        _rfqRoundRepository = rfqRoundRepository;
    }

    public async Task<RfqRoundResponse> CreateAsync(
        long rfqId,
        CreateRfqRoundRequest request,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0)
        {
            throw new ApiException("Invalid RFQ ID.", StatusCodes.Status400BadRequest);
        }

        ValidateDeadline(request.Deadline);

        var round = await _rfqRoundRepository.CreateAsync(
            rfqId, request.Deadline, actingUserId, ipAddress, cancellationToken);

        if (round is null)
        {
            throw new ApiException("RFQ not found.", StatusCodes.Status404NotFound);
        }

        return RfqRoundMapper.ToResponse(round);
    }

    public async Task<RfqRoundResponse> PublishAsync(
        long rfqId,
        long roundId,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(rfqId, roundId);

        var round = await _rfqRoundRepository.PublishAsync(rfqId, roundId, actingUserId, ipAddress, cancellationToken);
        if (round is null)
        {
            throw new ApiException("Round not found.", StatusCodes.Status404NotFound);
        }

        return RfqRoundMapper.ToResponse(round);
    }

    public async Task<RfqRoundResponse> CloseAsync(
        long rfqId,
        long roundId,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(rfqId, roundId);

        var round = await _rfqRoundRepository.CloseAsync(rfqId, roundId, actingUserId, ipAddress, cancellationToken);
        if (round is null)
        {
            throw new ApiException("Round not found.", StatusCodes.Status404NotFound);
        }

        return RfqRoundMapper.ToResponse(round);
    }

    public async Task<IReadOnlyList<RfqRoundResponse>> GetByRfqIdAsync(
        long rfqId,
        CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0)
        {
            throw new ApiException("Invalid RFQ ID.", StatusCodes.Status400BadRequest);
        }

        var rfq = await _rfqRepository.GetByIdAsync(rfqId, cancellationToken);
        if (rfq is null)
        {
            throw new ApiException("RFQ not found.", StatusCodes.Status404NotFound);
        }

        var rounds = await _rfqRoundRepository.GetByRfqIdAsync(rfqId, cancellationToken);
        return rounds.Select(RfqRoundMapper.ToResponse).ToList();
    }

    public async Task<RfqRoundResponse> GetByIdAsync(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(rfqId, roundId);

        var rfq = await _rfqRepository.GetByIdAsync(rfqId, cancellationToken);
        if (rfq is null)
        {
            throw new ApiException("RFQ not found.", StatusCodes.Status404NotFound);
        }

        var round = await _rfqRoundRepository.GetByIdAsync(rfqId, roundId, cancellationToken);
        if (round is null)
        {
            throw new ApiException("Round not found.", StatusCodes.Status404NotFound);
        }

        return RfqRoundMapper.ToResponse(round);
    }

    private static void ValidateDeadline(DateTimeOffset deadline)
    {
        if (deadline <= DateTimeOffset.UtcNow)
        {
            throw new ApiException("Deadline must be in the future.", StatusCodes.Status400BadRequest);
        }
    }

    private static void ValidateIds(long rfqId, long roundId)
    {
        if (rfqId <= 0 || roundId <= 0)
        {
            throw new ApiException("Invalid RFQ or round ID.", StatusCodes.Status400BadRequest);
        }
    }
}