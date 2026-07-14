using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<PaginatedResponse<UserWithRole>> GetUsersAsync(
        UserFilterRequest filter,
        CancellationToken cancellationToken = default);
}

public sealed class UserWithRole
{
    public required long Id { get; init; }
    public required string FullName { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public string? WebsiteUrl { get; init; }
    public required string Status { get; init; }
    public required string Role { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
}
