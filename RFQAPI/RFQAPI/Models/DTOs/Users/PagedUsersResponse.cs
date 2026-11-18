namespace RFQAPI.Models.DTOs.Users;

public class PagedUsersResponse
{
    public IReadOnlyList<UserResponse> Items { get; set; } = Array.Empty<UserResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}