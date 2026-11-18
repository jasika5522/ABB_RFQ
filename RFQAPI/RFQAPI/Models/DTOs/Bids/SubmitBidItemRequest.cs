using System.ComponentModel.DataAnnotations;

namespace RFQAPI.Models.DTOs.Bids;

public class SubmitBidItemRequest
{
    [Required]
    public long RfqItemId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than zero.")]
    public decimal UnitPrice { get; set; }
}