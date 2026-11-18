using Dapper;
using Npgsql;
using RFQAPI.DAL.DbConnection;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Evaluation;
using RFQAPI.Models.Entities;
using RFQAPI.Repositories.Interfaces;

namespace RFQAPI.Repositories.Implementations;

public class EvaluationRepository : IEvaluationRepository
{
    private readonly DapperContext _context;

    public EvaluationRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<EvaluateRoundResponse?> EvaluateRoundAsync(
        long rfqId,
        long roundId,
        long evaluatedBy,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _context.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string lockRfqSql = """
                SELECT rfq_id, rfq_number, status, currency_code
                FROM rfqs
                WHERE rfq_id = @RfqId
                FOR UPDATE
                """;

            var rfqRow = await connection.QuerySingleOrDefaultAsync<RfqEvalLockRow>(
                new CommandDefinition(lockRfqSql, new { RfqId = rfqId }, transaction, cancellationToken: cancellationToken));

            if (rfqRow is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            const string lockRoundSql = """
                SELECT round_id, rfq_id, round_number, status
                FROM rfq_rounds
                WHERE round_id = @RoundId AND rfq_id = @RfqId
                FOR UPDATE
                """;

            var roundRow = await connection.QuerySingleOrDefaultAsync<RfqRoundEvalLockRow>(
                new CommandDefinition(lockRoundSql, new { RoundId = roundId, RfqId = rfqId }, transaction,
                    cancellationToken: cancellationToken));

            if (roundRow is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(rfqRow.Status, RfqStatuses.Closed, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("The RFQ must be in Closed status before evaluation.", StatusCodes.Status400BadRequest);
            }

            if (!string.Equals(roundRow.Status, RoundStatuses.Closed, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Only closed rounds can be evaluated.", StatusCodes.Status400BadRequest);
            }

            var alreadyEvaluated = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT EXISTS (
                        SELECT 1
                        FROM bid_evaluations be
                        INNER JOIN bids b ON b.bid_id = be.bid_id
                        WHERE be.round_id = @RoundId AND be.rfq_id = @RfqId
                    )
                    """,
                    new { RoundId = roundId, RfqId = rfqId },
                    transaction,
                    cancellationToken: cancellationToken));

            if (alreadyEvaluated)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("This round has already been evaluated.", StatusCodes.Status409Conflict);
            }

            var incompleteBidId = await connection.QuerySingleOrDefaultAsync<long?>(
                new CommandDefinition(
                    """
                    SELECT b.bid_id
                    FROM bids b
                    WHERE b.rfq_id = @RfqId
                      AND b.round_id = @RoundId
                      AND b.status = @SubmittedStatus
                      AND (
                          SELECT COUNT(DISTINCT bi.rfq_item_id)
                          FROM bid_items bi
                          WHERE bi.bid_id = b.bid_id
                      ) <> (
                          SELECT COUNT(*)
                          FROM rfq_items ri
                          WHERE ri.rfq_id = @RfqId
                      )
                    LIMIT 1
                    """,
                    new
                    {
                        RfqId = rfqId,
                        RoundId = roundId,
                        SubmittedStatus = BidStatuses.Submitted
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (incompleteBidId is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException(
                    "One or more submitted bids do not include all mandatory RFQ items.",
                    StatusCodes.Status400BadRequest);
            }

            const string rankedSql = """
                SELECT
                    bid_id AS BidId,
                    supplier_id AS SupplierId,
                    total_price AS TotalPrice,
                    rank_position AS RankPosition
                FROM get_ranked_bids(@RfqId, @RoundId)
                """;

            var rankedBids = (await connection.QueryAsync<RankedBid>(
                new CommandDefinition(rankedSql, new { RfqId = rfqId, RoundId = roundId }, transaction,
                    cancellationToken: cancellationToken))).ToList();

            if (rankedBids.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("There are no eligible submitted bids to evaluate.", StatusCodes.Status400BadRequest);
            }

            const string insertEvalSql = """
                INSERT INTO bid_evaluations (bid_id, rfq_id, round_id, total_price, rank_position, evaluated_by)
                VALUES (@BidId, @RfqId, @RoundId, @TotalPrice, @RankPosition, @EvaluatedBy)
                """;

            foreach (var ranked in rankedBids)
            {
                var rank = ToRankInteger(ranked.RankPosition);
                await connection.ExecuteAsync(
                    new CommandDefinition(insertEvalSql, new
                    {
                        ranked.BidId,
                        RfqId = rfqId,
                        RoundId = roundId,
                        ranked.TotalPrice,
                        RankPosition = rank,
                        EvaluatedBy = evaluatedBy
                    }, transaction, cancellationToken: cancellationToken));
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE rfq_rounds
                    SET status = @EvaluatedStatus
                    WHERE round_id = @RoundId AND rfq_id = @RfqId
                    """,
                    new
                    {
                        RoundId = roundId,
                        RfqId = rfqId,
                        EvaluatedStatus = RoundStatuses.Evaluated
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE rfqs
                    SET status = @EvaluatedStatus,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE rfq_id = @RfqId
                    """,
                    new { RfqId = rfqId, EvaluatedStatus = RfqStatuses.Evaluated },
                    transaction,
                    cancellationToken: cancellationToken));

            var lowestRank = rankedBids.Min(r => r.RankPosition);
            var lowestRankCount = rankedBids.Count(r => r.RankPosition == lowestRank);
            var lowestPrice = rankedBids.Where(r => r.RankPosition == lowestRank).Min(r => r.TotalPrice);
            var isTied = lowestRankCount > 1;

            var evaluatedAt = await connection.ExecuteScalarAsync<DateTimeOffset>(
                new CommandDefinition(
                    """
                    SELECT MAX(evaluated_at)
                    FROM bid_evaluations
                    WHERE round_id = @RoundId AND rfq_id = @RfqId
                    """,
                    new { RoundId = roundId, RfqId = rfqId },
                    transaction,
                    cancellationToken: cancellationToken));

            await AuditLogHelper.InsertAsync(
                connection,
                transaction,
                evaluatedBy,
                AuditActions.RfqRoundEvaluated,
                AuditEntityTypes.RfqRound,
                roundId,
                oldValues: new
                {
                    rfq_status = rfqRow.Status,
                    round_status = roundRow.Status
                },
                newValues: new
                {
                    rfq_status = RfqStatuses.Evaluated,
                    round_status = RoundStatuses.Evaluated,
                    evaluated_bid_count = rankedBids.Count,
                    lowest_total_price = lowestPrice,
                    lowest_rank = ToRankInteger(lowestRank),
                    is_lowest_rank_tied = isTied,
                    lowest_rank_supplier_count = lowestRankCount
                },
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new EvaluateRoundResponse
            {
                RfqId = rfqId,
                RfqNumber = rfqRow.RfqNumber,
                RoundId = roundId,
                RoundNumber = roundRow.RoundNumber,
                CurrencyCode = rfqRow.CurrencyCode,
                EvaluatedAt = evaluatedAt,
                EvaluatedBidCount = rankedBids.Count,
                LowestTotalPrice = lowestPrice,
                LowestRank = ToRankInteger(lowestRank),
                IsLowestRankTied = isTied,
                LowestRankSupplierCount = lowestRankCount
            };
        }
        catch (ApiException)
        {
            throw;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ApiException("This round has already been evaluated.", StatusCodes.Status409Conflict);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<EvaluationResultsResponse?> GetResultsAsync(
        long rfqId,
        long roundId,
        CancellationToken cancellationToken = default)
    {
        const string contextSql = """
            SELECT
                rf.rfq_id AS RfqId,
                rf.rfq_number AS RfqNumber,
                rf.currency_code AS CurrencyCode,
                rr.round_id AS RoundId,
                rr.round_number AS RoundNumber
            FROM rfq_rounds rr
            INNER JOIN rfqs rf ON rf.rfq_id = rr.rfq_id
            WHERE rr.round_id = @RoundId AND rr.rfq_id = @RfqId
            """;

        await using var connection = _context.CreateConnection();

        var context = await connection.QuerySingleOrDefaultAsync<EvaluationContextRow>(
            new CommandDefinition(contextSql, new { RfqId = rfqId, RoundId = roundId }, cancellationToken: cancellationToken));

        if (context is null)
        {
            return null;
        }

        const string bidsSql = """
            SELECT
                be.bid_id AS BidId,
                b.supplier_id AS SupplierId,
                s.company_name AS CompanyName,
                s.registration_number AS RegistrationNumber,
                be.total_price AS TotalPrice,
                be.rank_position AS RankPosition,
                be.evaluated_at AS EvaluatedAt
            FROM bid_evaluations be
            INNER JOIN bids b ON b.bid_id = be.bid_id
            INNER JOIN suppliers s ON s.supplier_id = b.supplier_id
            WHERE be.rfq_id = @RfqId AND be.round_id = @RoundId
            ORDER BY be.rank_position, be.total_price, be.bid_id
            """;

        var bids = (await connection.QueryAsync<EvaluationBidRow>(
            new CommandDefinition(bidsSql, new { RfqId = rfqId, RoundId = roundId }, cancellationToken: cancellationToken)))
            .ToList();

        if (bids.Count == 0)
        {
            return null;
        }

        var lowestRank = bids.Min(b => b.RankPosition);
        var lowestRankBids = bids.Where(b => b.RankPosition == lowestRank).ToList();

        var summary = new EvaluationSummaryDto
        {
            RfqId = context.RfqId,
            RfqNumber = context.RfqNumber,
            RoundId = context.RoundId,
            RoundNumber = context.RoundNumber,
            CurrencyCode = context.CurrencyCode,
            EvaluatedAt = bids.Max(b => b.EvaluatedAt),
            LowestTotalPrice = lowestRankBids.Min(b => b.TotalPrice),
            LowestRank = lowestRank,
            IsLowestRankTied = lowestRankBids.Count > 1,
            LowestRankSupplierCount = lowestRankBids.Count
        };

        return new EvaluationResultsResponse
        {
            Summary = summary,
            Bids = bids.Select(b => EvaluationMapper.ToBidResponse(b, context.CurrencyCode)).ToList()
        };
    }

    public async Task<EvaluationBidDetailResponse?> GetBidDetailAsync(
        long rfqId,
        long roundId,
        long bidId,
        CancellationToken cancellationToken = default)
    {
        const string headerSql = """
            SELECT
                rf.rfq_id AS RfqId,
                rf.rfq_number AS RfqNumber,
                rf.currency_code AS CurrencyCode,
                rr.round_id AS RoundId,
                rr.round_number AS RoundNumber,
                be.bid_id AS BidId,
                b.supplier_id AS SupplierId,
                s.company_name AS CompanyName,
                s.registration_number AS RegistrationNumber,
                be.total_price AS TotalPrice,
                be.rank_position AS RankPosition,
                be.evaluated_at AS EvaluatedAt
            FROM bid_evaluations be
            INNER JOIN bids b ON b.bid_id = be.bid_id AND b.rfq_id = be.rfq_id AND b.round_id = be.round_id
            INNER JOIN rfqs rf ON rf.rfq_id = be.rfq_id
            INNER JOIN rfq_rounds rr ON rr.round_id = be.round_id AND rr.rfq_id = be.rfq_id
            INNER JOIN suppliers s ON s.supplier_id = b.supplier_id
            WHERE be.bid_id = @BidId AND be.rfq_id = @RfqId AND be.round_id = @RoundId
            """;

        await using var connection = _context.CreateConnection();

        var header = await connection.QuerySingleOrDefaultAsync<EvaluationBidDetailRow>(
            new CommandDefinition(headerSql, new { BidId = bidId, RfqId = rfqId, RoundId = roundId },
                cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        const string linesSql = """
            SELECT
                ri.rfq_item_id AS RfqItemId,
                ri.item_name AS ItemName,
                ri.quantity AS Quantity,
                ri.unit AS Unit,
                bi.unit_price AS UnitPrice,
                (ri.quantity * bi.unit_price) AS LineTotal
            FROM bid_items bi
            INNER JOIN rfq_items ri
                ON ri.rfq_item_id = bi.rfq_item_id AND ri.rfq_id = bi.rfq_id
            WHERE bi.bid_id = @BidId AND bi.rfq_id = @RfqId
            ORDER BY ri.rfq_item_id
            """;

        var items = (await connection.QueryAsync<EvaluationLineItem>(
            new CommandDefinition(linesSql, new { BidId = bidId, RfqId = rfqId }, cancellationToken: cancellationToken)))
            .ToList();

        return new EvaluationBidDetailResponse
        {
            RfqId = header.RfqId,
            RfqNumber = header.RfqNumber,
            RoundId = header.RoundId,
            RoundNumber = header.RoundNumber,
            BidId = header.BidId,
            SupplierId = header.SupplierId,
            CompanyName = header.CompanyName,
            RegistrationNumber = header.RegistrationNumber,
            TotalPrice = header.TotalPrice,
            CurrencyCode = header.CurrencyCode,
            RankPosition = header.RankPosition,
            EvaluatedAt = header.EvaluatedAt,
            Items = items.Select(EvaluationMapper.ToLineItem).ToList()
        };
    }

    private static int ToRankInteger(long rank)
    {
        if (rank is <= 0 or > int.MaxValue)
        {
            throw new ApiException("Invalid rank value returned from database.", StatusCodes.Status500InternalServerError);
        }

        return (int)rank;
    }

    private sealed class RfqEvalLockRow
    {
        public long RfqId { get; init; }
        public string RfqNumber { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string CurrencyCode { get; init; } = string.Empty;
    }

    private sealed class RfqRoundEvalLockRow
    {
        public long RoundId { get; init; }
        public long RfqId { get; init; }
        public int RoundNumber { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    private sealed class EvaluationContextRow
    {
        public long RfqId { get; init; }
        public string RfqNumber { get; init; } = string.Empty;
        public string CurrencyCode { get; init; } = string.Empty;
        public long RoundId { get; init; }
        public int RoundNumber { get; init; }
    }

    private sealed class EvaluationBidDetailRow
    {
        public long RfqId { get; init; }
        public string RfqNumber { get; init; } = string.Empty;
        public string CurrencyCode { get; init; } = string.Empty;
        public long RoundId { get; init; }
        public int RoundNumber { get; init; }
        public long BidId { get; init; }
        public long SupplierId { get; init; }
        public string CompanyName { get; init; } = string.Empty;
        public string RegistrationNumber { get; init; } = string.Empty;
        public decimal TotalPrice { get; init; }
        public int RankPosition { get; init; }
        public DateTimeOffset EvaluatedAt { get; init; }
    }
}