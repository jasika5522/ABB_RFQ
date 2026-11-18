namespace RFQAPI.Models.DTOs.Suppliers;

public class PagedSuppliersResponse
{
    public IReadOnlyList<SupplierDetailResponse> Items { get; set; } = Array.Empty<SupplierDetailResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}