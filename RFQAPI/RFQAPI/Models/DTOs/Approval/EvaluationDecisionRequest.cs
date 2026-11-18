using System.ComponentModel.DataAnnotations;

namespace RFQAPI.Models.DTOs.Approval;

public class EvaluationDecisionRequest
{
    [Required(ErrorMessage = "Decision is required.")]
    public string Decision { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Reason cannot exceed 2000 characters.")]
    public string? Reason { get; set; }
}