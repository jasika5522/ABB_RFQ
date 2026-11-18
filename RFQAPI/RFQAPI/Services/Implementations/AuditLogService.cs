using RFQAPI.Helpers;
using RFQAPI.Models.DTOs.AuditLogs;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 10;

    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PagedAuditLogsResponse> GetPagedAsync(
        AuditLogQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        NormalizeAndValidateQuery(query);

        var (items, totalCount) = await _auditLogRepository.GetPagedAsync(query, cancellationToken);
        return ToPagedResponse(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<AuditLogResponse> GetByIdAsync(long auditId, CancellationToken cancellationToken = default)
    {
        if (auditId <= 0)
        {
            throw new ApiException("Invalid audit log ID.", StatusCodes.Status400BadRequest);
        }

        var audit = await _auditLogRepository.GetByIdAsync(auditId, cancellationToken);
        if (audit is null)
        {
            throw new ApiException("Audit log not found.", StatusCodes.Status404NotFound);
        }

        return audit;
    }

    public async Task<PagedAuditLogsResponse> GetByEntityAsync(
        string entityType,
        long entityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (entityId <= 0)
        {
            throw new ApiException("Invalid entity ID.", StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ApiException("Entity type is required.", StatusCodes.Status400BadRequest);
        }

        var query = new AuditLogQueryParameters
        {
            Page = page,
            PageSize = pageSize,
            EntityType = entityType.Trim(),
            EntityId = entityId
        };

        NormalizeAndValidateQuery(query);

        var (items, totalCount) = await _auditLogRepository.GetByEntityAsync(
            query.EntityType!, entityId, query.Page, query.PageSize, cancellationToken);

        return ToPagedResponse(items, query.Page, query.PageSize, totalCount);
    }

    private static void NormalizeAndValidateQuery(AuditLogQueryParameters query)
    {
        if (query.Page < 1)
        {
            throw new ApiException("Page must be at least 1.", StatusCodes.Status400BadRequest);
        }

        if (query.PageSize < 1)
        {
            query.PageSize = DefaultPageSize;
        }

        if (query.PageSize > MaxPageSize)
        {
            throw new ApiException($"Page size must be between 1 and {MaxPageSize}.", StatusCodes.Status400BadRequest);
        }

        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
        {
            throw new ApiException("'fromDate' cannot be later than 'toDate'.", StatusCodes.Status400BadRequest);
        }
    }

    private static PagedAuditLogsResponse ToPagedResponse(
        IReadOnlyList<AuditLogResponse> items,
        int page,
        int pageSize,
        int totalCount)
    {
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedAuditLogsResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}