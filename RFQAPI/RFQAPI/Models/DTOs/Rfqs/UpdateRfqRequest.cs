using System.ComponentModel.DataAnnotations;

namespace RFQAPI.Models.DTOs.Rfqs;

public class UpdateRfqRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title is required.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Currency code is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters.")]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Currency code must be three letters.")]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one RFQ item is required.")]
    [MinLength(1, ErrorMessage = "At least one RFQ item is required.")]
    public List<RfqItemRequest> Items { get; set; } = [];
}