using RFQAPI.Helpers;
using RFQAPI.Models;
using RFQAPI.Models.DTOs.Suppliers;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Services.Implementations;

public class SupplierService : ISupplierService
{
    private const int MaxPageSize = 100;

    private static readonly HashSet<string> ReviewableStatuses =
    [
        SupplierApprovalStatuses.Approved,
        SupplierApprovalStatuses.Suspended
    ];

    private readonly ISupplierRepository _supplierRepository;

    public SupplierService(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<SupplierResponse> GetMyProfileAsync(long userId, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByUserIdAsync(userId, cancellationToken);
        if (supplier is null)
        {
            throw new ApiException("Supplier profile not found.", StatusCodes.Status404NotFound);
        }

        return SupplierMapper.ToResponse(supplier);
    }

    public async Task<SupplierResponse> UpdateMyProfileAsync(
        long userId,
        UpdateSupplierProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _supplierRepository.UpdateProfileAsync(
            userId,
            request.CompanyName,
            request.ContactPerson,
            request.Phone,
            cancellationToken);

        if (!updated)
        {
            throw new ApiException("Supplier profile not found.", StatusCodes.Status404NotFound);
        }

        var supplier = await _supplierRepository.GetByUserIdAsync(userId, cancellationToken);
        return SupplierMapper.ToResponse(supplier!);
    }

    public async Task<PagedSuppliersResponse> GetAllAsync(
        int page,
        int pageSize,
        string? approvalStatus,
        string? companyName,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ApiException("Page must be at least 1.", StatusCodes.Status400BadRequest);
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ApiException($"Page size must be between 1 and {MaxPageSize}.", StatusCodes.Status400BadRequest);
        }

        if (!string.IsNullOrWhiteSpace(approvalStatus) &&
            approvalStatus is not SupplierApprovalStatuses.Pending
                and not SupplierApprovalStatuses.Approved
                and not SupplierApprovalStatuses.Suspended)
        {
            throw new ApiException("Invalid approval status filter.", StatusCodes.Status400BadRequest);
        }

        var (items, totalCount) = await _supplierRepository.GetAllAsync(
            page, pageSize, approvalStatus, companyName, cancellationToken);

        return new PagedSuppliersResponse
        {
            Items = items.Select(SupplierMapper.ToDetailResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<SupplierDetailResponse> GetByIdAsync(long supplierId, CancellationToken cancellationToken = default)
    {
        if (supplierId <= 0)
        {
            throw new ApiException("Invalid supplier ID.", StatusCodes.Status400BadRequest);
        }

        var supplier = await _supplierRepository.GetByIdWithUserAsync(supplierId, cancellationToken);
        if (supplier is null)
        {
            throw new ApiException("Supplier not found.", StatusCodes.Status404NotFound);
        }

        return SupplierMapper.ToDetailResponse(supplier);
    }

    public async Task<SupplierDetailResponse> UpdateApprovalStatusAsync(
        long supplierId,
        long actingUserId,
        string approvalStatus,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (supplierId <= 0)
        {
            throw new ApiException("Invalid supplier ID.", StatusCodes.Status400BadRequest);
        }

        if (!ReviewableStatuses.Contains(approvalStatus))
        {
            throw new ApiException(
                "Approval status must be Approved or Suspended.",
                StatusCodes.Status400BadRequest);
        }

        try
        {
            var updated = await _supplierRepository.UpdateApprovalStatusWithAuditAsync(
                supplierId, approvalStatus, actingUserId, ipAddress, cancellationToken);

            if (!updated)
            {
                throw new ApiException("Supplier not found.", StatusCodes.Status404NotFound);
            }
        }
        catch (Npgsql.PostgresException)
        {
            throw new ApiException("Unable to update supplier status.", StatusCodes.Status400BadRequest);
        }

        return await GetByIdAsync(supplierId, cancellationToken);
    }
}