using Dapper;
using Npgsql;
using RFQAPI.DAL.DbConnection;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Awards;
using RFQAPI.Repositories.Interfaces;

namespace RFQAPI.Repositories.Implementations;

public class AwardRepository : IAwardRepository
{
    private const string AwardSelectColumns =
        """
        a.award_id AS AwardId,
        a.rfq_id AS RfqId,
        rf.rfq_number AS RfqNumber,
        rf.title AS RfqTitle,
        rf.currency_code AS CurrencyCode,
        a.round_id AS RoundId,
        rr.round_number AS RoundNumber,
        a.bid_id AS BidId,
        b.supplier_id AS SupplierId,
        s.company_name AS CompanyName,
        s.registration_number AS RegistrationNumber,
        be.total_price AS AwardedBidTotalPrice,
        a.awarded_by AS AwardedBy,
        a.award_comment AS AwardComment,
        a.awarded_at AS AwardedAt
        """;

    private readonly DapperContext _context;

    public AwardRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<AwardResponse> CreateAsync(
        long rfqId,
        long roundId,
        long bidId,
        string awardComment,
        long awardedBy,
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

            var awardExists = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    "SELECT EXISTS (SELECT 1 FROM awards WHERE rfq_id = @RfqId)",
                    new { RfqId = rfqId },
                    transaction,
                    cancellationToken: cancellationToken));

            if (awardExists)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("An award already exists for this RFQ.", StatusCodes.Status409Conflict);
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
                    "Awards are allowed only when the RFQ and round are in Evaluated status.",
                    StatusCodes.Status400BadRequest);
            }

            var decisionAction = await GetLatestDecisionActionAsync(connection, transaction, rfqId, roundId, cancellationToken);
            if (decisionAction is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException(
                    "An approved evaluation decision is required before creating an award.",
                    StatusCodes.Status400BadRequest);
            }

            if (string.Equals(decisionAction, AuditActions.EvaluationRejected, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Cannot award an RFQ with a rejected evaluation decision.", StatusCodes.Status400BadRequest);
            }

            if (!string.Equals(decisionAction, AuditActions.EvaluationApproved, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException(
                    "An approved evaluation decision is required before creating an award.",
                    StatusCodes.Status400BadRequest);
            }

            var tieStats = await GetLowestRankStatsAsync(connection, transaction, rfqId, roundId, cancellationToken);
            if (tieStats.LowestRankSupplierCount != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException(
                    "Cannot award when multiple suppliers share the lowest rank.",
                    StatusCodes.Status400BadRequest);
            }

            const string bidSql = """
                SELECT b.bid_id AS BidId, b.status AS Status, b.supplier_id AS SupplierId,
                       be.rank_position AS RankPosition, be.total_price AS TotalPrice
                FROM bids b
                INNER JOIN bid_evaluations be ON be.bid_id = b.bid_id
                WHERE b.bid_id = @BidId
                  AND b.rfq_id = @RfqId
                  AND b.round_id = @RoundId
                FOR UPDATE OF b
                """;

            var bid = await connection.QuerySingleOrDefaultAsync<BidAwardRow>(
                new CommandDefinition(bidSql, new { BidId = bidId, RfqId = rfqId, RoundId = roundId }, transaction,
                    cancellationToken: cancellationToken));

            if (bid is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Bid not found for the specified RFQ and round.", StatusCodes.Status404NotFound);
            }

            if (!string.Equals(bid.Status, BidStatuses.Submitted, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Only submitted bids can be awarded.", StatusCodes.Status400BadRequest);
            }

            if (bid.RankPosition != tieStats.LowestRank || bid.RankPosition != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("The selected bid must be the unique lowest-ranked bid.", StatusCodes.Status400BadRequest);
            }

            const string insertSql = """
                INSERT INTO awards (rfq_id, round_id, bid_id, awarded_by, award_comment)
                VALUES (@RfqId, @RoundId, @BidId, @AwardedBy, @AwardComment)
                RETURNING award_id
                """;

            var awardId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(insertSql, new
                {
                    RfqId = rfqId,
                    RoundId = roundId,
                    BidId = bidId,
                    AwardedBy = awardedBy,
                    AwardComment = awardComment.Trim()
                }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE rfqs
                    SET status = @AwardedStatus,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE rfq_id = @RfqId
                    """,
                    new { RfqId = rfqId, AwardedStatus = RfqStatuses.Awarded },
                    transaction,
                    cancellationToken: cancellationToken));

            await AuditLogHelper.InsertAsync(
                connection,
                transaction,
                awardedBy,
                AuditActions.RfqAwarded,
                AuditEntityTypes.Rfq,
                rfqId,
                oldValues: new { rfq_status = rfqStatus },
                newValues: new
                {
                    rfq_id = rfqId,
                    round_id = roundId,
                    bid_id = bidId,
                    supplier_id = bid.SupplierId,
                    awarded_by = awardedBy,
                    award_comment = awardComment.Trim(),
                    awarded_at = DateTimeOffset.UtcNow,
                    previous_rfq_status = rfqStatus,
                    new_rfq_status = RfqStatuses.Awarded,
                    awarded_bid_total_price = bid.TotalPrice
                },
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return await GetAwardResponseByIdAsync(connection, awardId, cancellationToken)
                ?? throw new ApiException("Award was created but could not be retrieved.", StatusCodes.Status500InternalServerError);
        }
        catch (ApiException)
        {
            throw;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ApiException("An award already exists for this RFQ or bid.", StatusCodes.Status409Conflict);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AwardResponse?> GetByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default)
    {
        await using var connection = _context.CreateConnection();

        const string sql = $"""
            SELECT {AwardSelectColumns}
            FROM awards a
            INNER JOIN rfqs rf ON rf.rfq_id = a.rfq_id
            INNER JOIN rfq_rounds rr ON rr.round_id = a.round_id AND rr.rfq_id = a.rfq_id
            INNER JOIN bids b ON b.bid_id = a.bid_id AND b.rfq_id = a.rfq_id AND b.round_id = a.round_id
            INNER JOIN suppliers s ON s.supplier_id = b.supplier_id
            INNER JOIN bid_evaluations be ON be.bid_id = a.bid_id
            WHERE a.rfq_id = @RfqId
            """;

        return await connection.QuerySingleOrDefaultAsync<AwardResponse>(
            new CommandDefinition(sql, new { RfqId = rfqId }, cancellationToken: cancellationToken));
    }

    public async Task<(IReadOnlyList<AwardResponse> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? rfqNumber,
        string? companyName,
        CancellationToken cancellationToken = default)
    {
        var whereClause = "WHERE 1=1";
        var parameters = new DynamicParameters();
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        if (!string.IsNullOrWhiteSpace(rfqNumber))
        {
            whereClause += " AND rf.rfq_number ILIKE @RfqNumberPattern";
            parameters.Add("RfqNumberPattern", $"%{rfqNumber.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(companyName))
        {
            whereClause += " AND s.company_name ILIKE @CompanyPattern";
            parameters.Add("CompanyPattern", $"%{companyName.Trim()}%");
        }

        var fromClause = """
            FROM awards a
            INNER JOIN rfqs rf ON rf.rfq_id = a.rfq_id
            INNER JOIN rfq_rounds rr ON rr.round_id = a.round_id AND rr.rfq_id = a.rfq_id
            INNER JOIN bids b ON b.bid_id = a.bid_id AND b.rfq_id = a.rfq_id AND b.round_id = a.round_id
            INNER JOIN suppliers s ON s.supplier_id = b.supplier_id
            INNER JOIN bid_evaluations be ON be.bid_id = a.bid_id
            """;

        var countSql = $"SELECT COUNT(*) {fromClause} {whereClause}";
        var sql = $"""
            SELECT {AwardSelectColumns}
            {fromClause}
            {whereClause}
            ORDER BY a.awarded_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        await using var connection = _context.CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<AwardResponse>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();

        return (items, totalCount);
    }

    private static async Task<string?> GetLatestDecisionActionAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long rfqId,
        long roundId,
        CancellationToken cancellationToken)
    {
        return await connection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(
                """
                SELECT action
                FROM audit_log
                WHERE entity_type = @EntityType
                  AND entity_id = @RoundId
                  AND action IN (@ApprovedAction, @RejectedAction)
                  AND (new_values->>'rfq_id')::bigint = @RfqId
                ORDER BY audit_id DESC
                LIMIT 1
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
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
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

    private static async Task<AwardResponse?> GetAwardResponseByIdAsync(
        NpgsqlConnection connection,
        long awardId,
        CancellationToken cancellationToken)
    {
        const string sql = $"""
            SELECT {AwardSelectColumns}
            FROM awards a
            INNER JOIN rfqs rf ON rf.rfq_id = a.rfq_id
            INNER JOIN rfq_rounds rr ON rr.round_id = a.round_id AND rr.rfq_id = a.rfq_id
            INNER JOIN bids b ON b.bid_id = a.bid_id AND b.rfq_id = a.rfq_id AND b.round_id = a.round_id
            INNER JOIN suppliers s ON s.supplier_id = b.supplier_id
            INNER JOIN bid_evaluations be ON be.bid_id = a.bid_id
            WHERE a.award_id = @AwardId
            """;

        return await connection.QuerySingleOrDefaultAsync<AwardResponse>(
            new CommandDefinition(sql, new { AwardId = awardId }, cancellationToken: cancellationToken));
    }

    private sealed class BidAwardRow
    {
        public long BidId { get; init; }
        public string Status { get; init; } = string.Empty;
        public long SupplierId { get; init; }
        public int RankPosition { get; init; }
        public decimal TotalPrice { get; init; }
    }

    private sealed class LowestRankStats
    {
        public int LowestRank { get; init; }
        public int LowestRankSupplierCount { get; init; }
        public decimal LowestTotalPrice { get; init; }
    }
}