namespace RFQAPI.Models.DTOs.Rfqs;

public class SupplierRfqEligibilityResponse
{
    public string ApprovalStatus { get; set; } = string.Empty;
    public bool CanSubmitBid { get; set; }
    public string? Message { get; set; }
}