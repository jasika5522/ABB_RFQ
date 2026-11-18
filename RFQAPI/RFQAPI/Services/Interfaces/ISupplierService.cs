using RFQAPI.Models.DTOs.Suppliers;

namespace RFQAPI.Services.Interfaces;

public interface ISupplierService
{
    Task<SupplierResponse> GetMyProfileAsync(long userId, CancellationToken cancellationToken = default);
    Task<SupplierResponse> UpdateMyProfileAsync(
        long userId,
        UpdateSupplierProfileRequest request,
        CancellationToken cancellationToken = default);
    Task<PagedSuppliersResponse> GetAllAsync(
        int page,
        int pageSize,
        string? approvalStatus,
        string? companyName,
        CancellationToken cancellationToken = default);
    Task<SupplierDetailResponse> GetByIdAsync(long supplierId, CancellationToken cancellationToken = default);
    Task<SupplierDetailResponse> UpdateApprovalStatusAsync(
        long supplierId,
        long actingUserId,
        string approvalStatus,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}