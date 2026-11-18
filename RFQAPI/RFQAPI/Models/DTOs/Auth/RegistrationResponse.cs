using RFQAPI.Models.DTOs.Suppliers;
using RFQAPI.Models.DTOs.Users;

namespace RFQAPI.Models.DTOs.Auth;

public class RegistrationResponse
{
    public UserResponse User { get; set; } = null!;
    public SupplierResponse Supplier { get; set; } = null!;
}