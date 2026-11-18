using Npgsql;
using RFQAPI.Models.Entities;

namespace RFQAPI.Repositories.Interfaces;

public interface ISupplierRepository
{
    Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken cancellationToken = default);
    Task<Supplier?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<SupplierWithUser?> GetByIdWithUserAsync(long supplierId, CancellationToken cancellationToken = default);
    Task<Supplier> CreateAsync(Supplier supplier, NpgsqlTransaction transaction, CancellationToken cancellationToken = default);
    Task<bool> UpdateProfileAsync(long userId, string companyName, string? contactPerson, string? phone, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SupplierWithUser> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? approvalStatus,
        string? companyNameContains,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateApprovalStatusWithAuditAsync(
        long supplierId,
        string newStatus,
        long actingUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}