using Dapper;
using Npgsql;
using RFQAPI.DAL.DbConnection;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Rfqs;
using RFQAPI.Models.Entities;
using RFQAPI.Repositories.Interfaces;

namespace RFQAPI.Repositories.Implementations;

public class RfqRepository : IRfqRepository
{
    private const string RfqSelectColumns =
        """
        rfq_id AS RfqId,
        rfq_number AS RfqNumber,
        title AS Title,
        description AS Description,
        created_by AS CreatedBy,
        status AS Status,
        currency_code AS CurrencyCode,
        created_at AS CreatedAt,
        updated_at AS UpdatedAt
        """;

    private const string RfqItemSelectColumns =
        """
        rfq_item_id AS RfqItemId,
        rfq_id AS RfqId,
        item_name AS ItemName,
        description AS Description,
        quantity AS Quantity,
        unit AS Unit,
        created_at AS CreatedAt
        """;

    private readonly DapperContext _context;

    public RfqRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<bool> RfqNumberExistsAsync(string rfqNumber, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (SELECT 1 FROM rfqs WHERE LOWER(rfq_number) = LOWER(@RfqNumber))
            """;

        await using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { RfqNumber = rfqNumber.Trim() }, cancellationToken: cancellationToken));
    }

    public async Task<Rfq?> GetByIdAsync(long rfqId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {RfqSelectColumns}
            FROM rfqs
            WHERE rfq_id = @RfqId
            """;

        await using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Rfq>(
            new CommandDefinition(sql, new { RfqId = rfqId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<RfqItem>> GetItemsByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {RfqItemSelectColumns}
            FROM rfq_items
            WHERE rfq_id = @RfqId
            ORDER BY rfq_item_id
            """;

        await using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<RfqItem>(
            new CommandDefinition(sql, new { RfqId = rfqId }, cancellationToken: cancellationToken));

        return items.ToList();
    }

    public async Task<RfqDetailResult?> CreateWithItemsAsync(
        CreateRfqRequest request,
        long createdBy,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _context.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string insertRfqSql = $"""
                INSERT INTO rfqs (rfq_number, title, description, created_by, status, currency_code)
                VALUES (@RfqNumber, @Title, @Description, @CreatedBy, @Status, @CurrencyCode)
                RETURNING {RfqSelectColumns}
                """;

            var rfq = await connection.QuerySingleAsync<Rfq>(
                new CommandDefinition(insertRfqSql, new
                {
                    RfqNumber = request.RfqNumber.Trim(),
                    Title = request.Title.Trim(),
                    Description = request.Description,
                    CreatedBy = createdBy,
                    Status = RfqStatuses.Draft,
                    CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant()
                }, transaction, cancellationToken: cancellationToken));

            var items = await InsertItemsAsync(connection, transaction, rfq.RfqId, request.Items, cancellationToken);

            await AuditLogHelper.InsertAsync(
                connection,
                transaction,
                createdBy,
                AuditActions.RfqCreated,
                AuditEntityTypes.Rfq,
                rfq.RfqId,
                oldValues: null,
                newValues: new
                {
                    rfq_number = rfq.RfqNumber,
                    title = rfq.Title,
                    status = rfq.Status,
                    currency_code = rfq.CurrencyCode,
                    item_count = items.Count
                },
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new RfqDetailResult { Rfq = rfq, Items = items };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RfqDetailResult?> UpdateDraftWithItemsAsync(
        long rfqId,
        UpdateRfqRequest request,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _context.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string lockSql = """
                SELECT
                    rfq_id AS RfqId,
                    rfq_number AS RfqNumber,
                    title AS Title,
                    description AS Description,
                    created_by AS CreatedBy,
                    status AS Status,
                    currency_code AS CurrencyCode,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM rfqs
                WHERE rfq_id = @RfqId
                FOR UPDATE
                """;

            var existing = await connection.QuerySingleOrDefaultAsync<Rfq>(
                new CommandDefinition(lockSql, new { RfqId = rfqId }, transaction, cancellationToken: cancellationToken));

            if (existing is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(existing.Status, RfqStatuses.Draft, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Only draft RFQs can be updated.", StatusCodes.Status400BadRequest);
            }

            if (existing.CreatedBy != actingUserId)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("You can update only RFQs that you created.", StatusCodes.Status400BadRequest);
            }

            var oldItemCount = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    "SELECT COUNT(*) FROM rfq_items WHERE rfq_id = @RfqId",
                    new { RfqId = rfqId },
                    transaction,
                    cancellationToken: cancellationToken));

            const string updateRfqSql = $"""
                UPDATE rfqs
                SET title = @Title,
                    description = @Description,
                    currency_code = @CurrencyCode,
                    updated_at = CURRENT_TIMESTAMP
                WHERE rfq_id = @RfqId
                RETURNING {RfqSelectColumns}
                """;

            var updatedRfq = await connection.QuerySingleAsync<Rfq>(
                new CommandDefinition(updateRfqSql, new
                {
                    RfqId = rfqId,
                    Title = request.Title.Trim(),
                    Description = request.Description,
                    CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant()
                }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM rfq_items WHERE rfq_id = @RfqId",
                    new { RfqId = rfqId },
                    transaction,
                    cancellationToken: cancellationToken));

            var newItems = await InsertItemsAsync(connection, transaction, rfqId, request.Items, cancellationToken);

            await AuditLogHelper.InsertAsync(
                connection,
                transaction,
                actingUserId,
                AuditActions.RfqUpdated,
                AuditEntityTypes.Rfq,
                rfqId,
                oldValues: new
                {
                    title = existing.Title,
                    description = existing.Description,
                    currency_code = existing.CurrencyCode,
                    item_count = oldItemCount
                },
                newValues: new
                {
                    title = updatedRfq.Title,
                    description = updatedRfq.Description,
                    currency_code = updatedRfq.CurrencyCode,
                    item_count = newItems.Count
                },
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new RfqDetailResult { Rfq = updatedRfq, Items = newItems };
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

    public async Task<(IReadOnlyList<Rfq> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? status,
        string? rfqNumber,
        string? title,
        CancellationToken cancellationToken = default)
    {
        var whereClause = "WHERE 1=1";
        var parameters = new DynamicParameters();
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        if (!string.IsNullOrWhiteSpace(status))
        {
            whereClause += " AND status = @Status";
            parameters.Add("Status", status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(rfqNumber))
        {
            whereClause += " AND rfq_number ILIKE @RfqNumberPattern";
            parameters.Add("RfqNumberPattern", $"%{rfqNumber.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            whereClause += " AND title ILIKE @TitlePattern";
            parameters.Add("TitlePattern", $"%{title.Trim()}%");
        }

        var countSql = $"SELECT COUNT(*) FROM rfqs {whereClause}";
        var sql = $"""
            SELECT {RfqSelectColumns}
            FROM rfqs
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        await using var connection = _context.CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<Rfq>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();

        return (items, totalCount);
    }

    private static async Task<List<RfqItem>> InsertItemsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long rfqId,
        IReadOnlyList<RfqItemRequest> items,
        CancellationToken cancellationToken)
    {
        const string insertItemSql = $"""
            INSERT INTO rfq_items (rfq_id, item_name, description, quantity, unit)
            VALUES (@RfqId, @ItemName, @Description, @Quantity, @Unit)
            RETURNING {RfqItemSelectColumns}
            """;

        var inserted = new List<RfqItem>();
        foreach (var item in items)
        {
            var row = await connection.QuerySingleAsync<RfqItem>(
                new CommandDefinition(insertItemSql, new
                {
                    RfqId = rfqId,
                    ItemName = item.ItemName.Trim(),
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Unit = item.Unit.Trim()
                }, transaction, cancellationToken: cancellationToken));

            inserted.Add(row);
        }

        return inserted;
    }
}