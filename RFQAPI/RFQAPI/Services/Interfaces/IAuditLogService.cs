using RFQAPI.Models.DTOs.AuditLogs;

namespace RFQAPI.Services.Interfaces;

public interface IAuditLogService
{
    Task<PagedAuditLogsResponse> GetPagedAsync(
        AuditLogQueryParameters query,
        CancellationToken cancellationToken = default);
    Task<AuditLogResponse> GetByIdAsync(long auditId, CancellationToken cancellationToken = default);
    Task<PagedAuditLogsResponse> GetByEntityAsync(
        string entityType,
        long entityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}