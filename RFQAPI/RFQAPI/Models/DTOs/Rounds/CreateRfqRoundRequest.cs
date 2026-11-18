using System.ComponentModel.DataAnnotations;

namespace RFQAPI.Models.DTOs.Rounds;

public class CreateRfqRoundRequest
{
    [Required(ErrorMessage = "Deadline is required.")]
    public DateTimeOffset Deadline { get; set; }
}