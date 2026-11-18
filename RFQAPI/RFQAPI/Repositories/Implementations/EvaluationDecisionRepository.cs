using System.Text.Json;
using Dapper;
using RFQAPI.DAL.DbConnection;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Approval;
using RFQAPI.Repositories.Interfaces;

namespace RFQAPI.Repositories.Implementations;

public class EvaluationDecisionRepository : IEvaluationDecisionRepository
{
    private readonly DapperContext _context;

    public EvaluationDecisionRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<EvaluationDecisionResponse> SubmitDecisionAsync(
        long rfqId,
        long roundId,
        long approverUserId,
        string decision,
        string? reason,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _context.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string lockRfqSql = """
                SELECT status FROM rfqs WHERE rfq_id = @RfqId FOR UPDATE
                """;

            var rfqStatus = await connection.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(lockRfqSql, new { RfqId = rfqId }, transaction, cancellationToken: cancellationToken));

            if (rfqStatus is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("RFQ not found.", StatusCodes.Status404NotFound);
            }

            const string lockRoundSql = """
                SELECT status FROM rfq_rounds
                WHERE round_id = @RoundId AND rfq_id = @RfqId
                FOR UPDATE
                """;

            var roundStatus = await connection.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(lockRoundSql, new { RoundId = roundId, RfqId = rfqId }, transaction,
                    cancellationToken: cancellationToken));

            if (roundStatus is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Round not found.", StatusCodes.Status404NotFound);
            }

            if (!string.Equals(rfqStatus, RfqStatuses.Evaluated, StringComparison.Ordinal) ||
                !string.Equals(roundStatus, RoundStatuses.Evaluated, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException(
                    "Evaluation decisions are allowed only when the RFQ and round are in Evaluated status.",
                    StatusCodes.Status400BadRequest);
            }

            var evaluationExists = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT EXISTS (
                        SELECT 1 FROM bid_evaluations
                        WHERE rfq_id = @RfqId AND round_id = @RoundId
                    )
                    """,
                    new { RfqId = rfqId, RoundId = roundId },
                    transaction,
                    cancellationToken: cancellationToken));

            if (!evaluationExists)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Evaluation has not been completed for this round.", StatusCodes.Status400BadRequest);
            }

            if (await DecisionExistsAsync(connection, transaction, rfqId, roundId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("A decision has already been recorded for this round.", StatusCodes.Status409Conflict);
            }

            var tieStats = await GetLowestRankStatsAsync(connection, transaction, rfqId, roundId, cancellationToken);

            if (string.Equals(decision, EvaluationDecisions.Approved, StringComparison.Ordinal))
            {
                if (tieStats.LowestRankSupplierCount > 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new ApiException(
                        "Cannot approve an evaluation when multiple suppliers share the lowest rank.",
                        StatusCodes.Status400BadRequest);
                }
            }
            else if (string.Equals(decision, EvaluationDecisions.Rejected, StringComparison.Ordinal))
            {
                if (tieStats.LowestRankSupplierCount <= 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new ApiException(
                        "Rejection is required only when multiple suppliers share the lowest rank.",
                        StatusCodes.Status400BadRequest);
                }

                if (string.IsNullOrWhiteSpace(reason))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new ApiException("A rejection reason is required.", StatusCodes.Status400BadRequest);
                }
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Decision must be Approved or Rejected.", StatusCodes.Status400BadRequest);
            }

            var auditAction = string.Equals(decision, EvaluationDecisions.Approved, StringComparison.Ordinal)
                ? AuditActions.EvaluationApproved
                : AuditActions.EvaluationRejected;

            var newValues = new
            {
                rfq_id = rfqId,
                round_id = roundId,
                decision,
                reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                approver_user_id = approverUserId,
                lowest_total_price = tieStats.LowestTotalPrice,
                lowest_rank = tieStats.LowestRank,
                lowest_rank_supplier_count = tieStats.LowestRankSupplierCount,
                is_lowest_rank_tied = tieStats.LowestRankSupplierCount > 1,
                decided_at = DateTimeOffset.UtcNow
            };

            await AuditLogHelper.InsertAsync(
                connection,
                transaction,
                approverUserId,
                auditAction,
                AuditEntityTypes.RfqRound,
                roundId,
                oldValues: new { decision = (string?)null },
                newValues,
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var record = await LoadLatestDecisionAsync(connection, rfqId, roundId, cancellationToken);
            return record ?? BuildResponseFromValues(newValues, decision);
        }
        catch (ApiException)
        {
            throw;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<EvaluationDecisionResponse?> GetDecisionAsync(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken = default)
    {
        const string contextSql = """
            SELECT 1
            FROM rfq_rounds
            WHERE round_id = @RoundId AND rfq_id = @RfqId
            """;

        await using var connection = _context.CreateConnection();

        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(contextSql, new { RfqId = rfqId, RoundId = roundId }, cancellationToken: cancellationToken));

        if (!exists)
        {
            return null;
        }

        var evaluationExists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT EXISTS (
                    SELECT 1 FROM bid_evaluations WHERE rfq_id = @RfqId AND round_id = @RoundId
                )
                """,
                new { RfqId = rfqId, RoundId = roundId },
                cancellationToken: cancellationToken));

        if (!evaluationExists)
        {
            throw new ApiException("Evaluation has not been completed for this round.", StatusCodes.Status400BadRequest);
        }

        var decision = await LoadLatestDecisionAsync(connection, rfqId, roundId, cancellationToken);
        if (decision is not null)
        {
            return decision;
        }

        var tieStats = await GetLowestRankStatsAsync(connection, null, rfqId, roundId, cancellationToken);

        return new EvaluationDecisionResponse
        {
            Status = EvaluationDecisionStatuses.Pending,
            RfqId = rfqId,
            RoundId = roundId,
            LowestTotalPrice = tieStats.LowestTotalPrice,
            LowestRank = tieStats.LowestRank,
            LowestRankSupplierCount = tieStats.LowestRankSupplierCount,
            IsLowestRankTied = tieStats.LowestRankSupplierCount > 1
        };
    }

    private static async Task<bool> DecisionExistsAsync(
        Npgsql.NpgsqlConnection connection,
        Npgsql.NpgsqlTransaction transaction,
        long rfqId,
        long roundId,
        CancellationToken cancellationToken)
    {
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM audit_log
                    WHERE entity_type = @EntityType
                      AND entity_id = @RoundId
                      AND action IN (@ApprovedAction, @RejectedAction)
                      AND (new_values->>'rfq_id')::bigint = @RfqId
                )
                """,
                new
                {
                    EntityType = AuditEntityTypes.RfqRound,
                    RoundId = roundId,
                    ApprovedAction = AuditActions.EvaluationApproved,
                    RejectedAction = AuditActions.EvaluationRejected,
                    RfqId = rfqId
                },
                transaction,
                cancellationToken: cancellationToken));
    }

    private static async Task<LowestRankStats> GetLowestRankStatsAsync(
        Npgsql.NpgsqlConnection connection,
        Npgsql.NpgsqlTransaction? transaction,
        long rfqId,
        long roundId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH lowest AS (
                SELECT MIN(rank_position) AS lowest_rank
                FROM bid_evaluations
                WHERE rfq_id = @RfqId AND round_id = @RoundId
            )
            SELECT
                l.lowest_rank AS LowestRank,
                COUNT(*)::int AS LowestRankSupplierCount,
                MIN(be.total_price) AS LowestTotalPrice
            FROM bid_evaluations be
            CROSS JOIN lowest l
            WHERE be.rfq_id = @RfqId
              AND be.round_id = @RoundId
              AND be.rank_position = l.lowest_rank
            GROUP BY l.lowest_rank
            """;

        var stats = await connection.QuerySingleOrDefaultAsync<LowestRankStats>(
            new CommandDefinition(sql, new { RfqId = rfqId, RoundId = roundId }, transaction,
                cancellationToken: cancellationToken));

        if (stats is null)
        {
            throw new ApiException("Evaluation records not found.", StatusCodes.Status400BadRequest);
        }

        return stats;
    }

    private static async Task<EvaluationDecisionResponse?> LoadLatestDecisionAsync(
        Npgsql.NpgsqlConnection connection,
        long rfqId,
        long roundId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT user_id, action, new_values, created_at
            FROM audit_log
            WHERE entity_type = @EntityType
              AND entity_id = @RoundId
              AND action IN (@ApprovedAction, @RejectedAction)
              AND (new_values->>'rfq_id')::bigint = @RfqId
            ORDER BY audit_id DESC
            LIMIT 1
            """;

        var row = await connection.QuerySingleOrDefaultAsync<AuditDecisionRow>(
            new CommandDefinition(sql,             new
            {
                EntityType = AuditEntityTypes.RfqRound,
                RoundId = roundId,
                ApprovedAction = AuditActions.EvaluationApproved,
                RejectedAction = AuditActions.EvaluationRejected,
                RfqId = rfqId
            }, cancellationToken: cancellationToken));

        if (row is null || string.IsNullOrWhiteSpace(row.NewValues))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(row.NewValues);
        var root = doc.RootElement;

        var decision = root.TryGetProperty("decision", out var decisionProp)
            ? decisionProp.GetString()
            : row.Action switch
            {
                var a when a == AuditActions.EvaluationApproved => EvaluationDecisions.Approved,
                var a when a == AuditActions.EvaluationRejected => EvaluationDecisions.Rejected,
                _ => null
            };

        var status = decision switch
        {
            EvaluationDecisions.Approved => EvaluationDecisionStatuses.Approved,
            EvaluationDecisions.Rejected => EvaluationDecisionStatuses.Rejected,
            _ => EvaluationDecisionStatuses.Pending
        };

        return new EvaluationDecisionResponse
        {
            Status = status,
            RfqId = rfqId,
            RoundId = roundId,
            Decision = decision,
            Reason = root.TryGetProperty("reason", out var reasonProp) && reasonProp.ValueKind != JsonValueKind.Null
                ? reasonProp.GetString()
                : null,
            ApproverUserId = row.UserId,
            DecidedAt = row.CreatedAt,
            LowestTotalPrice = root.TryGetProperty("lowest_total_price", out var priceProp)
                ? priceProp.GetDecimal()
                : null,
            LowestRank = root.TryGetProperty("lowest_rank", out var rankProp)
                ? rankProp.GetInt32()
                : null,
            LowestRankSupplierCount = root.TryGetProperty("lowest_rank_supplier_count", out var countProp)
                ? countProp.GetInt32()
                : null,
            IsLowestRankTied = root.TryGetProperty("is_lowest_rank_tied", out var tiedProp) && tiedProp.GetBoolean()
        };
    }

    private static EvaluationDecisionResponse BuildResponseFromValues(object newValues, string decision)
    {
        var json = JsonSerializer.Serialize(newValues);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var status = string.Equals(decision, EvaluationDecisions.Approved, StringComparison.Ordinal)
            ? EvaluationDecisionStatuses.Approved
            : EvaluationDecisionStatuses.Rejected;

        return new EvaluationDecisionResponse
        {
            Status = status,
            RfqId = root.GetProperty("rfq_id").GetInt64(),
            RoundId = root.GetProperty("round_id").GetInt64(),
            Decision = root.GetProperty("decision").GetString(),
            Reason = root.TryGetProperty("reason", out var r) && r.ValueKind != JsonValueKind.Null ? r.GetString() : null,
            ApproverUserId = root.GetProperty("approver_user_id").GetInt64(),
            DecidedAt = root.GetProperty("decided_at").GetDateTimeOffset(),
            LowestTotalPrice = root.GetProperty("lowest_total_price").GetDecimal(),
            LowestRank = root.GetProperty("lowest_rank").GetInt32(),
            LowestRankSupplierCount = root.GetProperty("lowest_rank_supplier_count").GetInt32(),
            IsLowestRankTied = root.GetProperty("is_lowest_rank_tied").GetBoolean()
        };
    }

    private sealed class LowestRankStats
    {
        public int LowestRank { get; init; }
        public int LowestRankSupplierCount { get; init; }
        public decimal LowestTotalPrice { get; init; }
    }

    private sealed class AuditDecisionRow
    {
        public long? UserId { get; init; }
        public string Action { get; init; } = string.Empty;
        public string? NewValues { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}