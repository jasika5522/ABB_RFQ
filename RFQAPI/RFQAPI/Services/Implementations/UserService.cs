using RFQAPI.Helpers;
using RFQAPI.Models.DTOs.Users;
using RFQAPI.Repositories.Interfaces;
using RFQAPI.Services.Interfaces;

namespace RFQAPI.Services.Implementations;

public class UserService : IUserService
{
    private const int MaxPageSize = 100;

    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponse> GetByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ApiException("Invalid user ID.", StatusCodes.Status400BadRequest);
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new ApiException("User not found.", StatusCodes.Status404NotFound);
        }

        return UserMapper.ToResponse(user);
    }

    public async Task<PagedUsersResponse> GetAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ApiException("Page must be at least 1.", StatusCodes.Status400BadRequest);
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ApiException($"Page size must be between 1 and {MaxPageSize}.", StatusCodes.Status400BadRequest);
        }

        var (items, totalCount) = await _userRepository.GetAllAsync(page, pageSize, cancellationToken);

        return new PagedUsersResponse
        {
            Items = items.Select(UserMapper.ToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UserResponse> UpdateActiveStatusAsync(
        long targetUserId,
        long actingUserId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (targetUserId <= 0)
        {
            throw new ApiException("Invalid user ID.", StatusCodes.Status400BadRequest);
        }

        if (targetUserId == actingUserId && !isActive)
        {
            throw new ApiException("You cannot deactivate your own account.", StatusCodes.Status400BadRequest);
        }

        var updated = await _userRepository.UpdateActiveStatusAsync(targetUserId, isActive, cancellationToken);
        if (!updated)
        {
            throw new ApiException("User not found.", StatusCodes.Status404NotFound);
        }

        var user = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        return UserMapper.ToResponse(user!);
    }
}