using Dapper;
using Npgsql;
using RFQAPI.DAL.DbConnection;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Bids;
using RFQAPI.Models.Entities;
using RFQAPI.Repositories.Interfaces;

namespace RFQAPI.Repositories.Implementations;

public class BidRepository : IBidRepository
{
    private const string BidSelectColumns =
        """
        bid_id AS BidId,
        rfq_id AS RfqId,
        round_id AS RoundId,
        supplier_id AS SupplierId,
        status AS Status,
        submitted_at AS SubmittedAt,
        withdrawn_at AS WithdrawnAt,
        created_at AS CreatedAt
        """;

    private const string BidItemSelectColumns =
        """
        bi.bid_item_id AS BidItemId,
        bi.bid_id AS BidId,
        bi.rfq_id AS RfqId,
        bi.rfq_item_id AS RfqItemId,
        bi.unit_price AS UnitPrice,
        bi.created_at AS CreatedAt,
        ri.item_name AS ItemName,
        ri.unit AS Unit,
        ri.quantity AS RfqQuantity
        """;

    private readonly DapperContext _context;

    public BidRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<long>> GetRfqItemIdsAsync(long rfqId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT rfq_item_id
            FROM rfq_items
            WHERE rfq_id = @RfqId
            ORDER BY rfq_item_id
            """;

        await using var connection = _context.CreateConnection();
        var ids = await connection.QueryAsync<long>(
            new CommandDefinition(sql, new { RfqId = rfqId }, cancellationToken: cancellationToken));

        return ids.ToList();
    }

    public async Task<BidDetailData?> SubmitAsync(
        long rfqId,
        long roundId,
        long supplierId,
        long actingUserId,
        IReadOnlyList<SubmitBidItemRequest> items,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _context.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string supplierSql = """
                SELECT approval_status
                FROM suppliers
                WHERE supplier_id = @SupplierId
                FOR UPDATE
                """;

            var approvalStatus = await connection.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(supplierSql, new { SupplierId = supplierId }, transaction,
                    cancellationToken: cancellationToken));

            if (approvalStatus is null ||
                !string.Equals(approvalStatus, SupplierApprovalStatuses.Approved, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException(
                    approvalStatus is null
                        ? "Supplier profile not found."
                        : "Only approved suppliers can submit bids.",
                    approvalStatus is null ? StatusCodes.Status404NotFound : StatusCodes.Status403Forbidden);
            }

            const string roundLockSql = """
                SELECT
                    r.round_id AS RoundId,
                    r.rfq_id AS RfqId,
                    r.status AS RoundStatus,
                    r.deadline AS Deadline,
                    rf.status AS RfqStatus,
                    rf.rfq_number AS RfqNumber,
                    rf.title AS RfqTitle,
                    rf.currency_code AS CurrencyCode,
                    r.round_number AS RoundNumber
                FROM rfq_rounds r
                INNER JOIN rfqs rf ON rf.rfq_id = r.rfq_id
                WHERE r.round_id = @RoundId AND r.rfq_id = @RfqId
                FOR UPDATE
                """;

            var roundContext = await connection.QuerySingleOrDefaultAsync<RoundBidContext>(
                new CommandDefinition(roundLockSql, new { RfqId = rfqId, RoundId = roundId }, transaction,
                    cancellationToken: cancellationToken));

            if (roundContext is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(roundContext.RfqStatus, RfqStatuses.Published, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("The RFQ is not published.", StatusCodes.Status400BadRequest);
            }

            if (!string.Equals(roundContext.RoundStatus, RoundStatuses.Published, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("The bidding round is not published.", StatusCodes.Status400BadRequest);
            }

            var deadlineValid = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    """
                    SELECT @Deadline > CURRENT_TIMESTAMP
                    FROM rfq_rounds
                    WHERE round_id = @RoundId AND rfq_id = @RfqId
                    """,
                    new { roundContext.Deadline, RfqId = rfqId, RoundId = roundId },
                    transaction,
                    cancellationToken: cancellationToken));

            if (!deadlineValid)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("The bidding deadline has passed.", StatusCodes.Status400BadRequest);
            }

            const string insertBidSql = $"""
                INSERT INTO bids (rfq_id, round_id, supplier_id, status)
                VALUES (@RfqId, @RoundId, @SupplierId, @Status)
                RETURNING {BidSelectColumns}
                """;

            var bid = await connection.QuerySingleAsync<Bid>(
                new CommandDefinition(insertBidSql, new
                {
                    RfqId = rfqId,
                    RoundId = roundId,
                    SupplierId = supplierId,
                    Status = BidStatuses.Submitted
                }, transaction, cancellationToken: cancellationToken));

            const string insertItemSql = """
                INSERT INTO bid_items (bid_id, rfq_id, rfq_item_id, unit_price)
                VALUES (@BidId, @RfqId, @RfqItemId, @UnitPrice)
                """;

            foreach (var item in items)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(insertItemSql, new
                    {
                        bid.BidId,
                        RfqId = rfqId,
                        item.RfqItemId,
                        item.UnitPrice
                    }, transaction, cancellationToken: cancellationToken));
            }

            await AuditLogHelper.InsertAsync(
                connection,
                transaction,
                actingUserId,
                AuditActions.BidSubmitted,
                AuditEntityTypes.Bid,
                bid.BidId,
                oldValues: null,
                newValues: new
                {
                    rfq_id = rfqId,
                    round_id = roundId,
                    supplier_id = supplierId,
                    status = bid.Status,
                    item_count = items.Count
                },
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var detailItems = await LoadBidItemsAsync(connection, bid.BidId, cancellationToken);
            return new BidDetailData
            {
                Bid = bid,
                RfqNumber = roundContext.RfqNumber,
                RfqTitle = roundContext.RfqTitle,
                CurrencyCode = roundContext.CurrencyCode,
                RoundNumber = roundContext.RoundNumber,
                RoundStatus = roundContext.RoundStatus,
                RoundDeadline = roundContext.Deadline,
                Items = detailItems
            };
        }
        catch (ApiException)
        {
            throw;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ApiException(
                "You already have an active bid for this round.",
                StatusCodes.Status409Conflict);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BidDetailData?> WithdrawAsync(
        long bidId,
        long supplierId,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _context.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string lockSql = $"""
                SELECT
                    b.bid_id AS BidId,
                    b.rfq_id AS RfqId,
                    b.round_id AS RoundId,
                    b.supplier_id AS SupplierId,
                    b.status AS Status,
                    b.submitted_at AS SubmittedAt,
                    b.withdrawn_at AS WithdrawnAt,
                    b.created_at AS CreatedAt,
                    r.status AS RoundStatus,
                    r.deadline AS Deadline,
                    r.round_number AS RoundNumber,
                    rf.status AS RfqStatus,
                    rf.rfq_number AS RfqNumber,
                    rf.title AS RfqTitle,
                    rf.currency_code AS CurrencyCode
                FROM bids b
                INNER JOIN rfq_rounds r ON r.round_id = b.round_id AND r.rfq_id = b.rfq_id
                INNER JOIN rfqs rf ON rf.rfq_id = b.rfq_id
                WHERE b.bid_id = @BidId AND b.supplier_id = @SupplierId
                FOR UPDATE OF b
                """;

            var bidRow = await connection.QuerySingleOrDefaultAsync<BidWithdrawContext>(
                new CommandDefinition(lockSql, new { BidId = bidId, SupplierId = supplierId }, transaction,
                    cancellationToken: cancellationToken));

            if (bidRow is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(bidRow.Status, BidStatuses.Submitted, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Only submitted bids can be withdrawn.", StatusCodes.Status400BadRequest);
            }

            if (!string.Equals(bidRow.RfqStatus, RfqStatuses.Published, StringComparison.Ordinal) ||
                !string.Equals(bidRow.RoundStatus, RoundStatuses.Published, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("The RFQ round is no longer open for withdrawal.", StatusCodes.Status400BadRequest);
            }

            var deadlineValid = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    "SELECT @Deadline > CURRENT_TIMESTAMP",
                    new { bidRow.Deadline },
                    transaction,
                    cancellationToken: cancellationToken));

            if (!deadlineValid)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("The bidding deadline has passed.", StatusCodes.Status400BadRequest);
            }

            const string updateSql = $"""
                UPDATE bids
                SET status = @WithdrawnStatus,
                    withdrawn_at = CURRENT_TIMESTAMP
                WHERE bid_id = @BidId
                RETURNING {BidSelectColumns}
                """;

            var updatedBid = await connection.QuerySingleAsync<Bid>(
                new CommandDefinition(updateSql, new
                {
                    BidId = bidId,
                    WithdrawnStatus = BidStatuses.Withdrawn
                }, transaction, cancellationToken: cancellationToken));

            await AuditLogHelper.InsertAsync(
                connection,
                transaction,
                actingUserId,
                AuditActions.BidWithdrawn,
                AuditEntityTypes.Bid,
                bidId,
                oldValues: new { status = bidRow.Status, withdrawn_at = bidRow.WithdrawnAt },
                newValues: new { status = updatedBid.Status, withdrawn_at = updatedBid.WithdrawnAt },
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var detailItems = await LoadBidItemsAsync(connection, bidId, cancellationToken);
            return new BidDetailData
            {
                Bid = updatedBid,
                RfqNumber = bidRow.RfqNumber,
                RfqTitle = bidRow.RfqTitle,
                CurrencyCode = bidRow.CurrencyCode,
                RoundNumber = bidRow.RoundNumber,
                RoundStatus = bidRow.RoundStatus,
                RoundDeadline = bidRow.Deadline,
                Items = detailItems
            };
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

    public async Task<(IReadOnlyList<BidSummary> Items, int TotalCount)> GetMyBidsAsync(
        long supplierId,
        int page,
        int pageSize,
        long? rfqId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var whereClause = "WHERE b.supplier_id = @SupplierId";
        var parameters = new DynamicParameters();
        parameters.Add("SupplierId", supplierId);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        if (rfqId is > 0)
        {
            whereClause += " AND b.rfq_id = @RfqId";
            parameters.Add("RfqId", rfqId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereClause += " AND b.status = @Status";
            parameters.Add("Status", status.Trim());
        }

        const string selectColumns = """
            b.bid_id AS BidId,
            b.rfq_id AS RfqId,
            b.round_id AS RoundId,
            b.status AS Status,
            b.submitted_at AS SubmittedAt,
            b.withdrawn_at AS WithdrawnAt,
            rf.rfq_number AS RfqNumber,
            rf.title AS RfqTitle,
            rf.currency_code AS CurrencyCode,
            rr.round_number AS RoundNumber
            """;

        var countSql = $"""
            SELECT COUNT(*)
            FROM bids b
            INNER JOIN rfqs rf ON rf.rfq_id = b.rfq_id
            INNER JOIN rfq_rounds rr ON rr.round_id = b.round_id AND rr.rfq_id = b.rfq_id
            {whereClause}
            """;

        var sql = $"""
            SELECT {selectColumns}
            FROM bids b
            INNER JOIN rfqs rf ON rf.rfq_id = b.rfq_id
            INNER JOIN rfq_rounds rr ON rr.round_id = b.round_id AND rr.rfq_id = b.rfq_id
            {whereClause}
            ORDER BY b.submitted_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        await using var connection = _context.CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<BidSummary>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();

        return (items, totalCount);
    }

    public async Task<BidDetailData?> GetDetailForSupplierAsync(
        long bidId,
        long supplierId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                b.bid_id AS BidId,
                b.rfq_id AS RfqId,
                b.round_id AS RoundId,
                b.supplier_id AS SupplierId,
                b.status AS Status,
                b.submitted_at AS SubmittedAt,
                b.withdrawn_at AS WithdrawnAt,
                b.created_at AS CreatedAt,
                rf.rfq_number AS RfqNumber,
                rf.title AS RfqTitle,
                rf.currency_code AS CurrencyCode,
                rr.round_number AS RoundNumber,
                rr.status AS RoundStatus,
                rr.deadline AS Deadline
            FROM bids b
            INNER JOIN rfqs rf ON rf.rfq_id = b.rfq_id
            INNER JOIN rfq_rounds rr ON rr.round_id = b.round_id AND rr.rfq_id = b.rfq_id
            WHERE b.bid_id = @BidId AND b.supplier_id = @SupplierId
            """;

        await using var connection = _context.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<BidDetailRow>(
            new CommandDefinition(sql, new { BidId = bidId, SupplierId = supplierId }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var items = await LoadBidItemsAsync(connection, bidId, cancellationToken);

        return new BidDetailData
        {
            Bid = new Bid
            {
                BidId = row.BidId,
                RfqId = row.RfqId,
                RoundId = row.RoundId,
                SupplierId = row.SupplierId,
                Status = row.Status,
                SubmittedAt = row.SubmittedAt,
                WithdrawnAt = row.WithdrawnAt,
                CreatedAt = row.CreatedAt
            },
            RfqNumber = row.RfqNumber,
            RfqTitle = row.RfqTitle,
            CurrencyCode = row.CurrencyCode,
            RoundNumber = row.RoundNumber,
            RoundStatus = row.RoundStatus,
            RoundDeadline = row.Deadline,
            Items = items
        };
    }

    private async Task<IReadOnlyList<BidItemDetail>> LoadBidItemsAsync(
        NpgsqlConnection connection,
        long bidId,
        CancellationToken cancellationToken)
    {
        const string sql = $"""
            SELECT {BidItemSelectColumns}
            FROM bid_items bi
            INNER JOIN rfq_items ri ON ri.rfq_item_id = bi.rfq_item_id AND ri.rfq_id = bi.rfq_id
            WHERE bi.bid_id = @BidId
            ORDER BY bi.rfq_item_id
            """;

        var items = await connection.QueryAsync<BidItemDetail>(
            new CommandDefinition(sql, new { BidId = bidId }, cancellationToken: cancellationToken));

        return items.ToList();
    }

    private sealed class RoundBidContext
    {
        public long RoundId { get; init; }
        public long RfqId { get; init; }
        public string RoundStatus { get; init; } = string.Empty;
        public DateTimeOffset Deadline { get; init; }
        public string RfqStatus { get; init; } = string.Empty;
        public string RfqNumber { get; init; } = string.Empty;
        public string RfqTitle { get; init; } = string.Empty;
        public string CurrencyCode { get; init; } = string.Empty;
        public int RoundNumber { get; init; }
    }

    private sealed class BidWithdrawContext : Bid
    {
        public string RoundStatus { get; init; } = string.Empty;
        public DateTimeOffset Deadline { get; init; }
        public int RoundNumber { get; init; }
        public string RfqStatus { get; init; } = string.Empty;
        public string RfqNumber { get; init; } = string.Empty;
        public string RfqTitle { get; init; } = string.Empty;
        public string CurrencyCode { get; init; } = string.Empty;
    }

    private sealed class BidDetailRow : Bid
    {
        public string RfqNumber { get; init; } = string.Empty;
        public string RfqTitle { get; init; } = string.Empty;
        public string CurrencyCode { get; init; } = string.Empty;
        public int RoundNumber { get; init; }
        public string RoundStatus { get; init; } = string.Empty;
        public DateTimeOffset Deadline { get; init; }
    }
}