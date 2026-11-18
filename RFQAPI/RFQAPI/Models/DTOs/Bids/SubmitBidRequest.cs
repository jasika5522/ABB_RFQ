using System.ComponentModel.DataAnnotations;

namespace RFQAPI.Models.DTOs.Bids;

public class SubmitBidRequest
{
    [Required(ErrorMessage = "Bid items are required.")]
    [MinLength(1, ErrorMessage = "At least one bid item is required.")]
    public List<SubmitBidItemRequest> Items { get; set; } = [];
}