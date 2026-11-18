using RFQAPI.Models.DTOs.Evaluation;
using RFQAPI.Models.Entities;

namespace RFQAPI.Helpers;

public static class EvaluationMapper
{
    public static EvaluatedBidResponse ToBidResponse(EvaluationBidRow row, string currencyCode) => new()
    {
        BidId = row.BidId,
        SupplierId = row.SupplierId,
        CompanyName = row.CompanyName,
        RegistrationNumber = row.RegistrationNumber,
        TotalPrice = row.TotalPrice,
        CurrencyCode = currencyCode,
        RankPosition = row.RankPosition,
        EvaluatedAt = row.EvaluatedAt
    };

    public static EvaluationLineItemResponse ToLineItem(EvaluationLineItem item) => new()
    {
        RfqItemId = item.RfqItemId,
        ItemName = item.ItemName,
        Quantity = item.Quantity,
        Unit = item.Unit,
        UnitPrice = item.UnitPrice,
        LineTotal = item.LineTotal
    };
}