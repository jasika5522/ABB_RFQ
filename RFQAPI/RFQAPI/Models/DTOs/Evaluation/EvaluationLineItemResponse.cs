namespace RFQAPI.Models.DTOs.Evaluation;

public class EvaluationLineItemResponse
{
    public long RfqItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}