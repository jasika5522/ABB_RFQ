using RFQAPI.Models.Entities;

namespace RFQAPI.Repositories.Interfaces;

public interface ISupplierRfqRepository
{
    Task<(IReadOnlyList<SupplierAvailableRfqRow> Items, int TotalCount)> GetAvailableAsync(
        int page,
        int pageSize,
        string? rfqNumber,
        string? title,
        string? search,
        CancellationToken cancellationToken = default);

    Task<SupplierAvailableRfqDetailRow?> GetAvailableDetailAsync(
        long rfqId,
        CancellationToken cancellationToken = default);
}

public sealed class SupplierAvailableRfqRow
{
    public long RfqId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public long PublishedRoundId { get; set; }
    public int RoundNumber { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset Deadline { get; set; }
    public int MandatoryItemCount { get; set; }
}

public sealed class SupplierAvailableRfqDetailRow
{
    public long RfqId { get; set; }
    public string RfqNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public long PublishedRoundId { get; set; }
    public int RoundNumber { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset Deadline { get; set; }
}