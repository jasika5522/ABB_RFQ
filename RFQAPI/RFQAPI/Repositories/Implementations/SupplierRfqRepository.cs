using Dapper;
using RFQAPI.DAL.DbConnection;
using RFQAPI.Models;
using RFQAPI.Repositories.Interfaces;

namespace RFQAPI.Repositories.Implementations;

public class SupplierRfqRepository : ISupplierRfqRepository
{
    private const string AvailableFromClause = """
        FROM rfqs rf
        INNER JOIN rfq_rounds rr
            ON rr.rfq_id = rf.rfq_id
           AND rr.status = @RoundPublishedStatus
        WHERE rf.status = @RfqPublishedStatus
          AND rr.deadline > CURRENT_TIMESTAMP
        """;

    private readonly DapperContext _context;

    public SupplierRfqRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<SupplierAvailableRfqRow> Items, int TotalCount)> GetAvailableAsync(
        int page,
        int pageSize,
        string? rfqNumber,
        string? title,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var whereClause = AvailableFromClause;
        var parameters = new DynamicParameters();
        parameters.Add("RfqPublishedStatus", RfqStatuses.Published);
        parameters.Add("RoundPublishedStatus", RoundStatuses.Published);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause += " AND (rf.rfq_number ILIKE @SearchPattern OR rf.title ILIKE @SearchPattern)";
            parameters.Add("SearchPattern", $"%{search.Trim()}%");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(rfqNumber))
            {
                whereClause += " AND rf.rfq_number ILIKE @RfqNumberPattern";
                parameters.Add("RfqNumberPattern", $"%{rfqNumber.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                whereClause += " AND rf.title ILIKE @TitlePattern";
                parameters.Add("TitlePattern", $"%{title.Trim()}%");
            }
        }

        var countSql = $"SELECT COUNT(*) {whereClause}";
        var sql = $"""
            SELECT
                rf.rfq_id AS RfqId,
                rf.rfq_number AS RfqNumber,
                rf.title AS Title,
                rf.description AS Description,
                rf.currency_code AS CurrencyCode,
                rr.round_id AS PublishedRoundId,
                rr.round_number AS RoundNumber,
                rr.published_at AS PublishedAt,
                rr.deadline AS Deadline,
                (
                    SELECT COUNT(*)::int
                    FROM rfq_items ri
                    WHERE ri.rfq_id = rf.rfq_id
                ) AS MandatoryItemCount
            {whereClause}
            ORDER BY rr.deadline ASC, rf.rfq_id DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        await using var connection = _context.CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<SupplierAvailableRfqRow>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();

        return (items, totalCount);
    }

    public async Task<SupplierAvailableRfqDetailRow?> GetAvailableDetailAsync(
        long rfqId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                rf.rfq_id AS RfqId,
                rf.rfq_number AS RfqNumber,
                rf.title AS Title,
                rf.description AS Description,
                rf.currency_code AS CurrencyCode,
                rr.round_id AS PublishedRoundId,
                rr.round_number AS RoundNumber,
                rr.published_at AS PublishedAt,
                rr.deadline AS Deadline
            FROM rfqs rf
            INNER JOIN rfq_rounds rr
                ON rr.rfq_id = rf.rfq_id
               AND rr.status = @RoundPublishedStatus
            WHERE rf.rfq_id = @RfqId
              AND rf.status = @RfqPublishedStatus
              AND rr.deadline > CURRENT_TIMESTAMP
            """;

        await using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SupplierAvailableRfqDetailRow>(
            new CommandDefinition(
                sql,
                new
                {
                    RfqId = rfqId,
                    RfqPublishedStatus = RfqStatuses.Published,
                    RoundPublishedStatus = RoundStatuses.Published
                },
                cancellationToken: cancellationToken));
    }
}