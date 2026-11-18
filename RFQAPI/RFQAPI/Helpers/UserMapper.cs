using RFQAPI.Models.DTOs.Users;
using RFQAPI.Models.Entities;

namespace RFQAPI.Helpers;

public static class UserMapper
{
    public static UserResponse ToResponse(User user) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}