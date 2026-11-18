using RFQAPI.Models.DTOs.Auth;

namespace RFQAPI.Services.Interfaces;

public interface IAuthService
{
    Task<RegistrationResponse> RegisterSupplierAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}