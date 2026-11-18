namespace RFQAPI.Models.Entities;

public class SupplierWithUser : Supplier
{
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public bool UserIsActive { get; set; }
    public DateTimeOffset UserCreatedAt { get; set; }
}