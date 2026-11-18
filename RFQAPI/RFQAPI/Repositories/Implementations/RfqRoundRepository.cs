using Dapper;
using Npgsql;
using RFQAPI.DAL.DbConnection;
using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.Entities;
using RFQAPI.Repositories.Interfaces;

namespace RFQAPI.Repositories.Implementations;

public class RfqRoundRepository : IRfqRoundRepository
{
    private const string RoundSelectColumns =
        """
        round_id AS RoundId,
        rfq_id AS RfqId,
        round_number AS RoundNumber,
        status AS Status,
        published_at AS PublishedAt,
        deadline AS Deadline,
        closed_at AS ClosedAt,
        created_at AS CreatedAt
        """;

    private readonly DapperContext _context;

    public RfqRoundRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<RfqRound?> CreateAsync(
        long rfqId,
        DateTimeOffset deadline,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _context.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string lockRfqSql = """
                SELECT status
                FROM rfqs
                WHERE rfq_id = @RfqId
                FOR UPDATE
                """;

            var rfqStatus = await connection.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(lockRfqSql, new { RfqId = rfqId }, transaction, cancellationToken: cancellationToken));

            if (rfqStatus is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            const string latestRoundSql = $"""
                SELECT {RoundSelectColumns}
                FROM rfq_rounds
                WHERE rfq_id = @RfqId
                ORDER BY round_number DESC
                LIMIT 1
                FOR UPDATE
                """;

            var latestRound = await connection.QuerySingleOrDefaultAsync<RfqRound>(
                new CommandDefinition(latestRoundSql, new { RfqId = rfqId }, transaction, cancellationToken: cancellationToken));

            int nextRoundNumber;
            if (latestRound is null)
            {
                if (!string.Equals(rfqStatus, RfqStatuses.Draft, StringComparison.Ordinal))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new ApiException(
                        "The first round can be created only when the RFQ is in Draft status.",
                        StatusCodes.Status400BadRequest);
                }

                nextRoundNumber = 1;
            }
            else
            {
                if (latestRound.Status is RoundStatuses.Draft or RoundStatuses.Published)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new ApiException(
                        "Cannot create a new round while the latest round is still Draft or Published.",
                        StatusCodes.Status400BadRequest);
                }

                if (!string.Equals(rfqStatus, RfqStatuses.Evaluated, StringComparison.Ordinal) ||
                    !string.Equals(latestRound.Status, RoundStatuses.Evaluated, StringComparison.Ordinal))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new ApiException(
                        "A subsequent round requires the RFQ and its latest round to be in Evaluated status.",
                        StatusCodes.Status400BadRequest);
                }

                nextRoundNumber = latestRound.RoundNumber + 1;
            }

            const string insertSql = $"""
                INSERT INTO rfq_rounds (rfq_id, round_number, status, deadline)
                VALUES (@RfqId, @RoundNumber, @Status, @Deadline)
                RETURNING {RoundSelectColumns}
                """;

            var round = await connection.QuerySingleAsync<RfqRound>(
                new CommandDefinition(insertSql, new
                {
                    RfqId = rfqId,
                    RoundNumber = nextRoundNumber,
                    Status = RoundStatuses.Draft,
                    Deadline = deadline
                }, transaction, cancellationToken: cancellationToken));

            await AuditLogHelper.InsertAsync(
                connection,
                transaction,
                actingUserId,
                AuditActions.RfqRoundCreated,
                AuditEntityTypes.RfqRound,
                round.RoundId,
                oldValues: null,
                newValues: new
                {
                    rfq_id = rfqId,
                    round_number = round.RoundNumber,
                    status = round.Status,
                    deadline = round.Deadline
                },
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return round;
        }
        catch (ApiException)
        {
            throw;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ApiException(
                "A round with this number already exists for the RFQ.",
                StatusCodes.Status409Conflict);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RfqRound?> PublishAsync(
        long rfqId,
        long roundId,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _context.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var round = await LockRoundAsync(connection, transaction, rfqId, roundId, cancellationToken);
            if (round is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(round.Status, RoundStatuses.Draft, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Only draft rounds can be published.", StatusCodes.Status400BadRequest);
            }

            if (round.Deadline <= DateTimeOffset.UtcNow)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("The bidding deadline must be in the future.", StatusCodes.Status400BadRequest);
            }

            const string publishedCheckSql = """
                SELECT EXISTS (
                    SELECT 1 FROM rfq_rounds
                    WHERE rfq_id = @RfqId
                      AND status = @PublishedStatus
                      AND round_id <> @RoundId
                )
                """;

            var hasOtherPublished = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(publishedCheckSql, new
                {
                    RfqId = rfqId,
                    PublishedStatus = RoundStatuses.Published,
                    RoundId = roundId
                }, transaction, cancellationToken: cancellationToken));

            if (hasOtherPublished)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Another round for this RFQ is already published.", StatusCodes.Status400BadRequest);
            }

            const string updateRoundSql = $"""
                UPDATE rfq_rounds
                SET status = @PublishedStatus,
                    published_at = CURRENT_TIMESTAMP
                WHERE round_id = @RoundId AND rfq_id = @RfqId
                RETURNING {RoundSelectColumns}
                """;

            var updatedRound = await connection.QuerySingleAsync<RfqRound>(
                new CommandDefinition(updateRoundSql, new
                {
                    RfqId = rfqId,
                    RoundId = roundId,
                    PublishedStatus = RoundStatuses.Published
                }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE rfqs
                    SET status = @PublishedStatus,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE rfq_id = @RfqId
                    """,
                    new { RfqId = rfqId, PublishedStatus = RfqStatuses.Published },
                    transaction,
                    cancellationToken: cancellationToken));

            await AuditLogHelper.InsertAsync(
                connection,
                transaction,
                actingUserId,
                AuditActions.RfqRoundPublished,
                AuditEntityTypes.RfqRound,
                roundId,
                oldValues: new { status = round.Status, published_at = round.PublishedAt },
                newValues: new { status = updatedRound.Status, published_at = updatedRound.PublishedAt },
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return updatedRound;
        }
        catch (ApiException)
        {
            throw;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ApiException("Another round for this RFQ is already published.", StatusCodes.Status409Conflict);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RfqRound?> CloseAsync(
        long rfqId,
        long roundId,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _context.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var round = await LockRoundAsync(connection, transaction, rfqId, roundId, cancellationToken);
            if (round is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            if (!string.Equals(round.Status, RoundStatuses.Published, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApiException("Only published rounds can be closed.", StatusCodes.Status400BadRequest);
            }

            const string updateRoundSql = $"""
                UPDATE rfq_rounds
                SET status = @ClosedStatus,
                    closed_at = CURRENT_TIMESTAMP
                WHERE round_id = @RoundId AND rfq_id = @RfqId
                RETURNING {RoundSelectColumns}
                """;

            var updatedRound = await connection.QuerySingleAsync<RfqRound>(
                new CommandDefinition(updateRoundSql, new
                {
                    RfqId = rfqId,
                    RoundId = roundId,
                    ClosedStatus = RoundStatuses.Closed
                }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE rfqs
                    SET status = @ClosedStatus,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE rfq_id = @RfqId
                    """,
                    new { RfqId = rfqId, ClosedStatus = RfqStatuses.Closed },
                    transaction,
                    cancellationToken: cancellationToken));

            await AuditLogHelper.InsertAsync(
                connection,
                transaction,
                actingUserId,
                AuditActions.RfqRoundClosed,
                AuditEntityTypes.RfqRound,
                roundId,
                oldValues: new { status = round.Status, closed_at = round.ClosedAt },
                newValues: new { status = updatedRound.Status, closed_at = updatedRound.ClosedAt },
                ipAddress,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return updatedRound;
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

    public async Task<IReadOnlyList<RfqRound>> GetByRfqIdAsync(long rfqId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {RoundSelectColumns}
            FROM rfq_rounds
            WHERE rfq_id = @RfqId
            ORDER BY round_number
            """;

        await using var connection = _context.CreateConnection();
        var items = await connection.QueryAsync<RfqRound>(
            new CommandDefinition(sql, new { RfqId = rfqId }, cancellationToken: cancellationToken));

        return items.ToList();
    }

    public async Task<RfqRound?> GetByIdAsync(long rfqId, long roundId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {RoundSelectColumns}
            FROM rfq_rounds
            WHERE rfq_id = @RfqId AND round_id = @RoundId
            """;

        await using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<RfqRound>(
            new CommandDefinition(sql, new { RfqId = rfqId, RoundId = roundId }, cancellationToken: cancellationToken));
    }

    private static async Task<RfqRound?> LockRoundAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long rfqId,
        long roundId,
        CancellationToken cancellationToken)
    {
        const string sql = $"""
            SELECT {RoundSelectColumns}
            FROM rfq_rounds
            WHERE rfq_id = @RfqId AND round_id = @RoundId
            FOR UPDATE
            """;

        return await connection.QuerySingleOrDefaultAsync<RfqRound>(
            new CommandDefinition(sql, new { RfqId = rfqId, RoundId = roundId }, transaction, cancellationToken: cancellationToken));
    }
}