using System.Text.Json;
using RFQAPI.Models.DTOs.AuditLogs;
using RFQAPI.Models.Entities;

namespace RFQAPI.Helpers;

public static class AuditLogMapper
{
    public static AuditLogResponse ToResponse(AuditLogRow row) => new()
    {
        AuditId = row.AuditId,
        Action = row.Action,
        EntityType = row.EntityType,
        EntityId = row.EntityId,
        OldValues = ParseJson(row.OldValuesJson),
        NewValues = ParseJson(row.NewValuesJson),
        IpAddress = row.IpAddress,
        CreatedAt = row.CreatedAt,
        Actor = row.ActorUserId is null
            ? null
            : new AuditActorResponse
            {
                UserId = row.ActorUserId.Value,
                FullName = row.ActorFullName ?? string.Empty,
                Email = row.ActorEmail ?? string.Empty
            }
    };

    private static JsonElement? ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}