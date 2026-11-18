using RFQAPI.Models.DTOs.Users;

namespace RFQAPI.Services.Interfaces;

public interface IUserService
{
    Task<UserResponse> GetByIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<PagedUsersResponse> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateActiveStatusAsync(
        long targetUserId,
        long actingUserId,
        bool isActive,
        CancellationToken cancellationToken = default);
}