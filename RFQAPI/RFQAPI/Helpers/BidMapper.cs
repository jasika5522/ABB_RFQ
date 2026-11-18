using RFQAPI.Models.DTOs.Bids;
using RFQAPI.Models.Entities;

namespace RFQAPI.Helpers;

public static class BidMapper
{
    public static BidSummaryResponse ToSummary(BidSummary bid) => new()
    {
        BidId = bid.BidId,
        RfqId = bid.RfqId,
        RoundId = bid.RoundId,
        RfqNumber = bid.RfqNumber,
        RfqTitle = bid.RfqTitle,
        CurrencyCode = bid.CurrencyCode,
        RoundNumber = bid.RoundNumber,
        Status = bid.Status,
        SubmittedAt = bid.SubmittedAt,
        WithdrawnAt = bid.WithdrawnAt
    };

    public static BidItemResponse ToItemResponse(BidItemDetail item) => new()
    {
        BidItemId = item.BidItemId,
        RfqItemId = item.RfqItemId,
        ItemName = item.ItemName,
        RfqQuantity = item.RfqQuantity,
        Unit = item.Unit,
        UnitPrice = item.UnitPrice
    };

    public static BidResponse ToResponse(Bid bid, string? currencyCode, IReadOnlyList<BidItemDetail> items) => new()
    {
        BidId = bid.BidId,
        RfqId = bid.RfqId,
        RoundId = bid.RoundId,
        SupplierId = bid.SupplierId,
        Status = bid.Status,
        SubmittedAt = bid.SubmittedAt,
        WithdrawnAt = bid.WithdrawnAt,
        CurrencyCode = currencyCode,
        Items = items.Select(ToItemResponse).ToList()
    };
}