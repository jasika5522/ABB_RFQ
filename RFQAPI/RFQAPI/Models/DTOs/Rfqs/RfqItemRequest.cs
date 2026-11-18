using System.ComponentModel.DataAnnotations;

namespace RFQAPI.Models.DTOs.Rfqs;

public class RfqItemRequest
{
    [Required(ErrorMessage = "Item name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Item name is required.")]
    public string ItemName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Unit is required.")]
    [StringLength(30, MinimumLength = 1, ErrorMessage = "Unit is required.")]
    public string Unit { get; set; } = string.Empty;
}