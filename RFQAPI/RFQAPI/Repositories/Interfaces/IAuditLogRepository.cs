using RFQAPI.Models.DTOs.AuditLogs;

namespace RFQAPI.Repositories.Interfaces;

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLogResponse> Items, int TotalCount)> GetPagedAsync(
        AuditLogQueryParameters query,
        CancellationToken cancellationToken = default);
    Task<AuditLogResponse?> GetByIdAsync(long auditId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditLogResponse> Items, int TotalCount)> GetByEntityAsync(
        string entityType,
        long entityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}