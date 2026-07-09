using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.Interfaces.Services;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PaginatedResponse<UserListItemResponse>> GetUsersAsync(
        UserFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _userRepository.GetUsersAsync(filter, cancellationToken);

        var items = result.Items.Select(u => new UserListItemResponse
        {
            Id = u.Id,
            FullName = u.FullName,
            Username = u.Username,
            Email = u.Email,
            AvatarUrl = u.AvatarUrl,
            DisplayTitle = null,
            Bio = u.Bio,
            WebsiteUrl = u.WebsiteUrl,
            Status = u.Status,
            Role = u.Role,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            LastLoginAt = u.LastLoginAt
        }).ToList();

        return new PaginatedResponse<UserListItemResponse>
        {
            Items = items,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            HasNextPage = result.HasNextPage,
            HasPreviousPage = result.HasPreviousPage
        };
    }
}
