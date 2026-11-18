namespace RFQAPI.Models.DTOs.AuditLogs;

public class AuditLogQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public long? UserId { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
}