using System.ComponentModel.DataAnnotations;

namespace RFQAPI.Models.DTOs.Suppliers;

public class UpdateSupplierProfileRequest
{
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 200 characters.")]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Contact person cannot exceed 150 characters.")]
    public string? ContactPerson { get; set; }

    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
    public string? Phone { get; set; }
}