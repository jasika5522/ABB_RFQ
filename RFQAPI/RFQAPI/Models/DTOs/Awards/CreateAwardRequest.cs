using System.ComponentModel.DataAnnotations;

namespace RFQAPI.Models.DTOs.Awards;

public class CreateAwardRequest
{
    [Required]
    public long BidId { get; set; }

    [Required(ErrorMessage = "Award comment is required.")]
    [StringLength(4000, MinimumLength = 1, ErrorMessage = "Award comment is required.")]
    public string AwardComment { get; set; } = string.Empty;
}