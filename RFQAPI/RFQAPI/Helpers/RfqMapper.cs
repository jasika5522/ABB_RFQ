using RFQAPI.Models.DTOs.Rfqs;
using RFQAPI.Models.Entities;

namespace RFQAPI.Helpers;

public static class RfqMapper
{
    public static RfqDetailResponse ToDetail(Rfq rfq, IReadOnlyList<RfqItem> items) => new()
    {
        RfqId = rfq.RfqId,
        RfqNumber = rfq.RfqNumber,
        Title = rfq.Title,
        Description = rfq.Description,
        CurrencyCode = rfq.CurrencyCode,
        Status = rfq.Status,
        CreatedBy = rfq.CreatedBy,
        CreatedAt = rfq.CreatedAt,
        UpdatedAt = rfq.UpdatedAt,
        Items = items.Select(ToItemResponse).ToList()
    };

    public static RfqItemResponse ToItemResponse(RfqItem item) => new()
    {
        RfqItemId = item.RfqItemId,
        RfqId = item.RfqId,
        ItemName = item.ItemName,
        Description = item.Description,
        Quantity = item.Quantity,
        Unit = item.Unit,
        CreatedAt = item.CreatedAt
    };

    public static RfqSummaryResponse ToSummary(Rfq rfq) => new()
    {
        RfqId = rfq.RfqId,
        RfqNumber = rfq.RfqNumber,
        Title = rfq.Title,
        CurrencyCode = rfq.CurrencyCode,
        Status = rfq.Status,
        CreatedBy = rfq.CreatedBy,
        CreatedAt = rfq.CreatedAt,
        UpdatedAt = rfq.UpdatedAt
    };
}