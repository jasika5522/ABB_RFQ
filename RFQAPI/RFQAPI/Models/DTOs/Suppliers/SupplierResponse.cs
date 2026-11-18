namespace RFQAPI.Models.DTOs.Suppliers;

public class SupplierResponse
{
    public long SupplierId { get; set; }
    public long UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}