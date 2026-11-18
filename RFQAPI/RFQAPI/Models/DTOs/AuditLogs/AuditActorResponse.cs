namespace RFQAPI.Models.DTOs.AuditLogs;

public class AuditActorResponse
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}