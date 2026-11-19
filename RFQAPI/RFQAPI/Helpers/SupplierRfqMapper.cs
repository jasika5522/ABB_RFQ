using RFQAPI.Models.DTOs.Rfqs;
using RFQAPI.Models.Entities;
using RFQAPI.Repositories.Interfaces;

namespace RFQAPI.Helpers;

public static class SupplierRfqMapper
{
    public static SupplierAvailableRfqSummaryResponse ToSummary(SupplierAvailableRfqRow row) => new()
    {
        RfqId = row.RfqId,
        RfqNumber = row.RfqNumber,
        Title = row.Title,
        Description = row.Description,
        CurrencyCode = row.CurrencyCode,
        PublishedRoundId = row.PublishedRoundId,
        RoundNumber = row.RoundNumber,
        PublishedAt = row.PublishedAt,
        Deadline = row.Deadline,
        MandatoryItemCount = row.MandatoryItemCount
    };

    public static SupplierAvailableRfqDetailResponse ToDetail(
        SupplierAvailableRfqDetailRow row,
        IReadOnlyList<RfqItem> items,
        SupplierRfqEligibilityResponse eligibility) => new()
    {
        RfqId = row.RfqId,
        RfqNumber = row.RfqNumber,
        Title = row.Title,
        Description = row.Description,
        CurrencyCode = row.CurrencyCode,
        PublishedRoundId = row.PublishedRoundId,
        RoundNumber = row.RoundNumber,
        PublishedAt = row.PublishedAt,
        Deadline = row.Deadline,
        Items = items.Select(RfqMapper.ToItemResponse).ToList(),
        Eligibility = eligibility
    };
}