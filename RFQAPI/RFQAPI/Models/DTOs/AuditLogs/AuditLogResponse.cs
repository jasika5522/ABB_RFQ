using System.Text.Json;

namespace RFQAPI.Models.DTOs.AuditLogs;

public class AuditLogResponse
{
    public long AuditId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public JsonElement? OldValues { get; set; }
    public JsonElement? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public AuditActorResponse? Actor { get; set; }
}