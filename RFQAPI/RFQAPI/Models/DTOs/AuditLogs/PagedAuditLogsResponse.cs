namespace RFQAPI.Models.DTOs.AuditLogs;

public class PagedAuditLogsResponse
{
    public IReadOnlyList<AuditLogResponse> Items { get; set; } = Array.Empty<AuditLogResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}