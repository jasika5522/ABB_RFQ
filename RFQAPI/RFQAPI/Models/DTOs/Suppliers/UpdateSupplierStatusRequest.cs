using System.ComponentModel.DataAnnotations;

namespace RFQAPI.Models.DTOs.Suppliers;

public class UpdateSupplierStatusRequest
{
    [Required(ErrorMessage = "Approval status is required.")]
    public string ApprovalStatus { get; set; } = string.Empty;
}