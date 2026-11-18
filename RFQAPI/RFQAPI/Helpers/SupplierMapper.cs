using RFQAPI.Models.DTOs.Suppliers;
using RFQAPI.Models.Entities;

namespace RFQAPI.Helpers;

public static class SupplierMapper
{
    public static SupplierResponse ToResponse(Supplier supplier) => new()
    {
        SupplierId = supplier.SupplierId,
        UserId = supplier.UserId,
        CompanyName = supplier.CompanyName,
        RegistrationNumber = supplier.RegistrationNumber,
        ContactPerson = supplier.ContactPerson,
        Phone = supplier.Phone,
        ApprovalStatus = supplier.ApprovalStatus,
        CreatedAt = supplier.CreatedAt
    };

    public static SupplierDetailResponse ToDetailResponse(SupplierWithUser supplier) => new()
    {
        SupplierId = supplier.SupplierId,
        UserId = supplier.UserId,
        CompanyName = supplier.CompanyName,
        RegistrationNumber = supplier.RegistrationNumber,
        ContactPerson = supplier.ContactPerson,
        Phone = supplier.Phone,
        ApprovalStatus = supplier.ApprovalStatus,
        CreatedAt = supplier.CreatedAt,
        UserFullName = supplier.UserFullName,
        UserEmail = supplier.UserEmail,
        UserIsActive = supplier.UserIsActive,
        UserCreatedAt = supplier.UserCreatedAt
    };
}