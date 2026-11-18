using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Rfqs;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Services.Implementations;

public class RfqService : IRfqService
{
    private const int MaxPageSize = 100;

    private static readonly HashSet<string> ValidStatuses =
    [
        RfqStatuses.Draft,
        RfqStatuses.Published,
        RfqStatuses.Closed,
        RfqStatuses.Evaluated,
        RfqStatuses.Awarded
    ];

    private readonly IRfqRepository _rfqRepository;

    public RfqService(IRfqRepository rfqRepository)
    {
        _rfqRepository = rfqRepository;
    }

    public async Task<RfqDetailResponse> CreateAsync(
        CreateRfqRequest request,
        long createdBy,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        ValidateCurrency(request.CurrencyCode);

        if (await _rfqRepository.RfqNumberExistsAsync(request.RfqNumber, cancellationToken))
        {
            throw new ApiException("An RFQ with this number already exists.", StatusCodes.Status409Conflict);
        }

        try
        {
            var result = await _rfqRepository.CreateWithItemsAsync(request, createdBy, ipAddress, cancellationToken);
            return RfqMapper.ToDetail(result!.Rfq, result.Items);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation)
        {
            throw new ApiException("An RFQ with this number already exists.", StatusCodes.Status409Conflict);
        }
    }

    public async Task<RfqDetailResponse> UpdateDraftAsync(
        long rfqId,
        UpdateRfqRequest request,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (rfqId <= 0)
        {
            throw new ApiException("Invalid RFQ ID.", StatusCodes.Status400BadRequest);
        }

        ValidateCurrency(request.CurrencyCode);

        var result = await _rfqRepository.UpdateDraftWithItemsAsync(
            rfqId, request, actingUserId, ipAddress, cancellationToken);

        if (result is null)
        {
            throw new ApiException("RFQ not found.", StatusCodes.Status404NotFound);
        }

        return RfqMapper.ToDetail(result.Rfq, result.Items);
    }

    public async Task<RfqDetailResponse> GetByIdAsync(long rfqId, CancellationToken cancellationToken = default)
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

        var items = await _rfqRepository.GetItemsByRfqIdAsync(rfqId, cancellationToken);
        return RfqMapper.ToDetail(rfq, items);
    }

    public async Task<PagedRfqsResponse> GetAllAsync(
        int page,
        int pageSize,
        string? status,
        string? rfqNumber,
        string? title,
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

        if (!string.IsNullOrWhiteSpace(status) && !ValidStatuses.Contains(status))
        {
            throw new ApiException("Invalid status filter.", StatusCodes.Status400BadRequest);
        }

        var (items, totalCount) = await _rfqRepository.GetAllAsync(
            page, pageSize, status, rfqNumber, title, cancellationToken);

        return new PagedRfqsResponse
        {
            Items = items.Select(RfqMapper.ToSummary).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static void ValidateCurrency(string currencyCode)
    {
        if (currencyCode.Length != 3 || !currencyCode.All(char.IsAsciiLetter))
        {
            throw new ApiException("Currency code must be three letters.", StatusCodes.Status400BadRequest);
        }
    }
}