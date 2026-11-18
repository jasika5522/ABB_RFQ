using RFQAPI.Helpers;
using RFQAPI.Models.DTOs.Awards;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Services.Implementations;

public class AwardService : IAwardService
{
    private const int MaxPageSize = 100;

    private readonly IAwardRepository _awardRepository;

    public AwardService(IAwardRepository awardRepository)
    {
        _awardRepository = awardRepository;
    }

    public async Task<AwardResponse> CreateAsync(
        long rfqId,
        long roundId,
        CreateAwardRequest request,
        long awardedBy,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0 || roundId <= 0 || request.BidId <= 0)
        {
            throw new ApiException("Invalid RFQ, round, or bid ID.", StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(request.AwardComment))
        {
            throw new ApiException("Award comment is required.", StatusCodes.Status400BadRequest);
        }

        return await _awardRepository.CreateAsync(
            rfqId,
            roundId,
            request.BidId,
            request.AwardComment,
            awardedBy,
            ipAddress,
            cancellationToken);
    }

    public async Task<AwardResponse> GetByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0)
        {
            throw new ApiException("Invalid RFQ ID.", StatusCodes.Status400BadRequest);
        }

        var award = await _awardRepository.GetByRfqIdAsync(rfqId, cancellationToken);
        if (award is null)
        {
            throw new ApiException("Award not found.", StatusCodes.Status404NotFound);
        }

        return award;
    }

    public async Task<PagedAwardsResponse> GetAllAsync(
        int page,
        int pageSize,
        string? rfqNumber,
        string? companyName,
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

        var (items, totalCount) = await _awardRepository.GetAllAsync(
            page, pageSize, rfqNumber, companyName, cancellationToken);

        return new PagedAwardsResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}