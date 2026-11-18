using Dapper;
using RFQAPI.DAL.DbConnection;
using RFQAPI.Helpers;
using RFQAPI.Models.DTOs.AuditLogs;
using RFQAPI.Models.Entities;
using RFQAPI.Repositories.Interfaces;

namespace RFQAPI.Repositories.Implementations;

public class AuditLogRepository : IAuditLogRepository
{
    private const string AuditSelectColumns =
        """
        a.audit_id AS AuditId,
        a.user_id AS ActorUserId,
        a.action AS Action,
        a.entity_type AS EntityType,
        a.entity_id AS EntityId,
        a.old_values::text AS OldValuesJson,
        a.new_values::text AS NewValuesJson,
        a.ip_address::text AS IpAddress,
        a.created_at AS CreatedAt,
        u.full_name AS ActorFullName,
        u.email AS ActorEmail
        """;

    private const string FromClause =
        """
        FROM audit_log a
        LEFT JOIN users u ON u.user_id = a.user_id
        """;

    private readonly DapperContext _context;

    public AuditLogRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<AuditLogResponse> Items, int TotalCount)> GetPagedAsync(
        AuditLogQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var (whereClause, parameters) = BuildFilterClause(query);
        parameters.Add("PageSize", query.PageSize);
        parameters.Add("Offset", (query.Page - 1) * query.PageSize);

        var countSql = $"SELECT COUNT(*) {FromClause} {whereClause}";
        var dataSql = $"""
            SELECT {AuditSelectColumns}
            {FromClause}
            {whereClause}
            ORDER BY a.created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        await using var connection = _context.CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var rows = (await connection.QueryAsync<AuditLogRow>(
            new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))).ToList();

        return (rows.Select(AuditLogMapper.ToResponse).ToList(), totalCount);
    }

    public async Task<AuditLogResponse?> GetByIdAsync(long auditId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT {AuditSelectColumns}
            {FromClause}
            WHERE a.audit_id = @AuditId
            """;

        await using var connection = _context.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<AuditLogRow>(
            new CommandDefinition(sql, new { AuditId = auditId }, cancellationToken: cancellationToken));

        return row is null ? null : AuditLogMapper.ToResponse(row);
    }

    public async Task<(IReadOnlyList<AuditLogResponse> Items, int TotalCount)> GetByEntityAsync(
        string entityType,
        long entityId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = new AuditLogQueryParameters
        {
            Page = page,
            PageSize = pageSize,
            EntityType = entityType,
            EntityId = entityId
        };

        return await GetPagedAsync(query, cancellationToken);
    }

    private static (string WhereClause, DynamicParameters Parameters) BuildFilterClause(AuditLogQueryParameters query)
    {
        var parameters = new DynamicParameters();
        var conditions = new List<string> { "1=1" };

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            conditions.Add("a.action = @Action");
            parameters.Add("Action", query.Action.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            conditions.Add("a.entity_type = @EntityType");
            parameters.Add("EntityType", query.EntityType.Trim());
        }

        if (query.EntityId is > 0)
        {
            conditions.Add("a.entity_id = @EntityId");
            parameters.Add("EntityId", query.EntityId.Value);
        }

        if (query.UserId is > 0)
        {
            conditions.Add("a.user_id = @UserId");
            parameters.Add("UserId", query.UserId.Value);
        }

        if (query.FromDate.HasValue)
        {
            conditions.Add("a.created_at >= @FromDate");
            parameters.Add("FromDate", query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            conditions.Add("a.created_at <= @ToDate");
            parameters.Add("ToDate", query.ToDate.Value);
        }

        return ($"WHERE {string.Join(" AND ", conditions)}", parameters);
    }
}