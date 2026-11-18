using System.Text.Json;
using Dapper;
using Npgsql;
using RFQAPI.DAL.DbConnection;
using RFQAPI.Models;
using RFQAPI.Models.Entities;
using RFQAPI.Repositories.Interfaces;

namespace RFQAPI.Repositories.Implementations;

public class SupplierRepository : ISupplierRepository
{
    private const string SupplierSelectColumns =
        """
        supplier_id AS SupplierId,
        user_id AS UserId,
        company_name AS CompanyName,
        registration_number AS RegistrationNumber,
        contact_person AS ContactPerson,
        phone AS Phone,
        approval_status AS ApprovalStatus,
        created_at AS CreatedAt
        """;

    private const string SupplierWithUserSelectColumns =
        """
        s.supplier_id AS SupplierId,
        s.user_id AS UserId,
        s.company_name AS CompanyName,
        s.registration_number AS RegistrationNumber,
        s.contact_person AS ContactPerson,
        s.phone AS Phone,
        s.approval_status AS ApprovalStatus,
        s.created_at AS CreatedAt,
        u.full_name AS UserFullName,
        u.email AS UserEmail,
        u.is_active AS UserIsActive,
        u.created_at AS UserCreatedAt
        """;

    private readonly DapperContext _context;

    public SupplierRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<bool> RegistrationNumberExistsAsync(
        string registrationNumber,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM suppliers WHERE LOWER(registration_number) = LOWER(@RegistrationNumber)
            )
            """;

        await using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { RegistrationNumber = registrationNumber.Trim() }, cancellationToken: cancellationToken));
    }

    public async Task<Supplier?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {SupplierSelectColumns}
            FROM suppliers
            WHERE user_id = @UserId
            """;

        await using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Supplier>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<SupplierWithUser?> GetByIdWithUserAsync(long supplierId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {SupplierWithUserSelectColumns}
            FROM suppliers s
            INNER JOIN users u ON u.user_id = s.user_id
            WHERE s.supplier_id = @SupplierId
            """;

        await using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<SupplierWithUser>(
            new CommandDefinition(sql, new { SupplierId = supplierId }, cancellationToken: cancellationToken));
    }

    public async Task<Supplier> CreateAsync(
        Supplier supplier,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            INSERT INTO suppliers (user_id, company_name, registration_number, contact_person, phone, approval_status)
            VALUES (@UserId, @CompanyName, @RegistrationNumber, @ContactPerson, @Phone, @ApprovalStatus)
            RETURNING {SupplierSelectColumns}
            """;

        return await transaction.Connection!.QuerySingleAsync<Supplier>(
            new CommandDefinition(sql, new
            {
                supplier.UserId,
                supplier.CompanyName,
                RegistrationNumber = supplier.RegistrationNumber.Trim(),
                supplier.ContactPerson,
                supplier.Phone,
                supplier.ApprovalStatus
            }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateProfileAsync(
        long userId,
        string companyName,
        string? contactPerson,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE suppliers
            SET company_name = @CompanyName,
                contact_person = @ContactPerson,
                phone = @Phone
            WHERE user_id = @UserId
            """;

        await using var connection = _context.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                UserId = userId,
                CompanyName = companyName.Trim(),
                ContactPerson = contactPerson,
                Phone = phone
            }, cancellationToken: cancellationToken));

        return rows > 0;
    }

    public async Task<(IReadOnlyList<SupplierWithUser> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? approvalStatus,
        string? companyNameContains,
        CancellationToken cancellationToken = default)
    {
        var whereClause = "WHERE 1=1";
        var parameters = new DynamicParameters();
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        if (!string.IsNullOrWhiteSpace(approvalStatus))
        {
            whereClause += " AND s.approval_status = @ApprovalStatus";
            parameters.Add("ApprovalStatus", approvalStatus.Trim());
        }

        if (!string.IsNullOrWhiteSpace(companyNameContains))
        {
            whereClause += " AND s.company_name ILIKE @CompanyNamePattern";
            parameters.Add("CompanyNamePattern", $"%{companyNameContains.Trim()}%");
        }

        var countSql = $"""
            SELECT COUNT(*)
            FROM suppliers s
            INNER JOIN users u ON u.user_id = s.user_id
            {whereClause}
            """;

        var sql = $"""
            SELECT {SupplierWithUserSelectColumns}
            FROM suppliers s
            INNER JOIN users u ON u.user_id = s.user_id
            {whereClause}
            ORDER BY s.created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        await using var connection = _context.CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<SupplierWithUser>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();

        return (items, totalCount);
    }

    public async Task<bool> UpdateApprovalStatusWithAuditAsync(
        long supplierId,
        string newStatus,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _context.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string selectSql = """
                SELECT approval_status
                FROM suppliers
                WHERE supplier_id = @SupplierId
                FOR UPDATE
                """;

            var oldStatus = await connection.QuerySingleOrDefaultAsync<string?>(
                new CommandDefinition(selectSql, new { SupplierId = supplierId }, transaction, cancellationToken: cancellationToken));

            if (oldStatus is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            const string updateSql = """
                UPDATE suppliers
                SET approval_status = @ApprovalStatus
                WHERE supplier_id = @SupplierId
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(updateSql, new { SupplierId = supplierId, ApprovalStatus = newStatus }, transaction,
                    cancellationToken: cancellationToken));

            var oldValues = JsonSerializer.Serialize(new { approval_status = oldStatus });
            var newValues = JsonSerializer.Serialize(new { approval_status = newStatus });

            const string auditSql = """
                INSERT INTO audit_log (user_id, action, entity_type, entity_id, old_values, new_values, ip_address)
                VALUES (@UserId, @Action, @EntityType, @EntityId, @OldValues::jsonb, @NewValues::jsonb, @IpAddress::inet)
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(auditSql, new
                {
                    UserId = actingUserId,
                    Action = AuditActions.SupplierStatusUpdated,
                    EntityType = AuditEntityTypes.Supplier,
                    EntityId = supplierId,
                    OldValues = oldValues,
                    NewValues = newValues,
                    IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress
                }, transaction, cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}