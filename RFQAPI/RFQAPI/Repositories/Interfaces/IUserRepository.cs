using Npgsql;
using RFQAPI.Models.Entities;

namespace RFQAPI.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, NpgsqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateActiveStatusAsync(long userId, bool isActive, CancellationToken cancellationToken = default);
}