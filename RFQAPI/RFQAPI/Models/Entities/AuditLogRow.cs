namespace RFQAPI.Models.Entities;

public class AuditLogRow
{
    public long AuditId { get; set; }
    public long? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? ActorFullName { get; set; }
    public string? ActorEmail { get; set; }
}