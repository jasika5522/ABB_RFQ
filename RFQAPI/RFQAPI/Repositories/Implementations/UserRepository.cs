using Dapper;
using Npgsql;
using RFQAPI.DAL.DbConnection;
using RFQAPI.Models.Entities;
using RFQAPI.Repositories.Interfaces;

namespace RFQAPI.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private const string UserSelectColumns =
        """
        user_id AS UserId,
        full_name AS FullName,
        email AS Email,
        password_hash AS PasswordHash,
        role AS Role,
        is_active AS IsActive,
        created_at AS CreatedAt
        """;

    private readonly DapperContext _context;

    public UserRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {UserSelectColumns}
            FROM users
            WHERE user_id = @UserId
            """;

        await using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            SELECT {UserSelectColumns}
            FROM users
            WHERE LOWER(email) = LOWER(@Email)
            """;

        await using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Email = email.Trim() }, cancellationToken: cancellationToken));
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1 FROM users WHERE LOWER(email) = LOWER(@Email)
            )
            """;

        await using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Email = email.Trim() }, cancellationToken: cancellationToken));
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            INSERT INTO users (full_name, email, password_hash, role, is_active)
            VALUES (@FullName, @Email, @PasswordHash, @Role, @IsActive)
            RETURNING {UserSelectColumns}
            """;

        await using var connection = _context.CreateConnection();
        return await connection.QuerySingleAsync<User>(
            new CommandDefinition(sql, new
            {
                user.FullName,
                Email = user.Email.Trim(),
                user.PasswordHash,
                user.Role,
                user.IsActive
            }, cancellationToken: cancellationToken));
    }

    public async Task<User> CreateAsync(
        User user,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = $"""
            INSERT INTO users (full_name, email, password_hash, role, is_active)
            VALUES (@FullName, @Email, @PasswordHash, @Role, @IsActive)
            RETURNING {UserSelectColumns}
            """;

        return await transaction.Connection!.QuerySingleAsync<User>(
            new CommandDefinition(sql, new
            {
                user.FullName,
                Email = user.Email.Trim(),
                user.PasswordHash,
                user.Role,
                user.IsActive
            }, transaction, cancellationToken: cancellationToken));
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        const string countSql = "SELECT COUNT(*) FROM users";

        const string sql = $"""
            SELECT {UserSelectColumns}
            FROM users
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        await using var connection = _context.CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, cancellationToken: cancellationToken));

        var items = (await connection.QueryAsync<User>(
            new CommandDefinition(sql, new
            {
                PageSize = pageSize,
                Offset = (page - 1) * pageSize
            }, cancellationToken: cancellationToken))).ToList();

        return (items, totalCount);
    }

    public async Task<bool> UpdateActiveStatusAsync(
        long userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE users
            SET is_active = @IsActive
            WHERE user_id = @UserId
            """;

        await using var connection = _context.CreateConnection();
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId, IsActive = isActive }, cancellationToken: cancellationToken));

        return rows > 0;
    }
}