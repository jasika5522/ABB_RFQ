using System.Text.Json;
using Dapper;
using Npgsql;

namespace RFQAPI.Helpers;

public static class AuditLogHelper
{
    public static async Task InsertAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long? userId,
        string action,
        string entityType,
        long entityId,
        object? oldValues,
        object? newValues,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        const string auditSql = """
            INSERT INTO audit_log (user_id, action, entity_type, entity_id, old_values, new_values, ip_address)
            VALUES (@UserId, @Action, @EntityType, @EntityId, @OldValues::jsonb, @NewValues::jsonb, @IpAddress::inet)
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(auditSql, new
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
                NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
                IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress
            }, transaction, cancellationToken: cancellationToken));
    }
}