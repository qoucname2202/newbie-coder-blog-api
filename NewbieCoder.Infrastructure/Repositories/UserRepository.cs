using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Repositories;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    private static readonly string[] AllowedSortFields =
    [
        "created_at",
        "updated_at",
        "full_name",
        "username",
        "email",
        "status"
    ];

    public async Task<PaginatedResponse<UserWithRole>> GetUsersAsync(
        UserFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles.Where(ur => ur.Status == UserRoleStatus.Active))
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        query = ApplySearch(query, filter.Keyword);
        query = ApplyStatusFilter(query, filter.Status);
        query = ApplySorting(query, filter.SortBy, filter.SortDirection);

        var totalCount = await query.CountAsync(cancellationToken);

        var skip = (filter.Page - 1) * filter.PageSize;
        var items = await query
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(u => new UserWithRole
            {
                Id = u.Id,
                FullName = u.FullName,
                Username = u.Username,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                WebsiteUrl = null,
                Status = u.Status.ToString(),
                Role = u.UserRoles
                    .Where(ur => ur.Status == UserRoleStatus.Active)
                    .Select(ur => ur.Role.Code)
                    .FirstOrDefault() ?? "USER",
                CreatedAt = u.EffDate,
                UpdatedAt = u.DateLastMaint,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

        return new PaginatedResponse<UserWithRole>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = filter.Page < totalPages,
            HasPreviousPage = filter.Page > 1
        };
    }

    private static IQueryable<User> ApplySearch(IQueryable<User> query, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return query;

        var term = keyword.Trim().ToLower();
        return query.Where(u =>
            u.FullName.ToLower().Contains(term) ||
            u.Username.ToLower().Contains(term) ||
            u.Email.ToLower().Contains(term));
    }

    private static IQueryable<User> ApplyStatusFilter(IQueryable<User> query, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return query;

        if (Enum.TryParse<UserStatus>(status, ignoreCase: true, out var userStatus))
            return query.Where(u => u.Status == userStatus);

        return query;
    }

    private static IQueryable<User> ApplySorting(IQueryable<User> query, string? sortBy, string sortDirection)
    {
        var field = (sortBy ?? "created_at").ToLowerInvariant();
        var direction = sortDirection?.ToLowerInvariant() == "asc"
            ? SortDirection.Asc
            : SortDirection.Desc;

        if (!AllowedSortFields.Contains(field))
            field = "created_at";

        query = field switch
        {
            "full_name" => direction == SortDirection.Asc
                ? query.OrderBy(u => u.FullName)
                : query.OrderByDescending(u => u.FullName),
            "username" => direction == SortDirection.Asc
                ? query.OrderBy(u => u.Username)
                : query.OrderByDescending(u => u.Username),
            "email" => direction == SortDirection.Asc
                ? query.OrderBy(u => u.Email)
                : query.OrderByDescending(u => u.Email),
            "status" => direction == SortDirection.Asc
                ? query.OrderBy(u => u.Status)
                : query.OrderByDescending(u => u.Status),
            "updated_at" => direction == SortDirection.Asc
                ? query.OrderBy(u => u.DateLastMaint)
                : query.OrderByDescending(u => u.DateLastMaint),
            _ => direction == SortDirection.Asc
                ? query.OrderBy(u => u.EffDate)
                : query.OrderByDescending(u => u.EffDate)
        };

        return query;
    }
}
