using Microsoft.EntityFrameworkCore;
using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.Enums;
using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Core.ViewModels;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.Repositories;

public sealed class RoleRepository(AppDbContext context) : IRoleRepository
{
    private static readonly string[] AllowedSortFields =
    [
        "name",
        "createdAt",
        "updatedAt"
    ];

    public async Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await context.Roles
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null, cancellationToken);
    }

    public async Task<RoleDetailResponse?> GetDetailByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var role = await context.Roles
            .AsNoTracking()
            .Include(r => r.UserRoles.Where(ur => ur.Status == UserRoleStatus.Active))
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null, cancellationToken);

        if (role is null)
            return null;

        var createdByUserId = await GetCreatorUserIdAsync(role, cancellationToken);

        return new RoleDetailResponse
        {
            Id = role.Id,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystem,
            IsActive = role.Status == RoleStatus.Active,
            UserCount = role.UserRoles.Count,
            CreatedAt = role.EffDate,
            CreatedBy = createdByUserId,
            UpdatedAt = role.DateLastMaint,
            UpdatedBy = null
        };
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpperInvariant();
        return await context.Roles
            .AsNoTracking()
            .AnyAsync(r =>
                r.Name.ToUpper() == normalizedName &&
                r.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> ExistsByNameExcludingIdAsync(
        long id,
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpperInvariant();
        return await context.Roles
            .AsNoTracking()
            .AnyAsync(r =>
                r.Id != id &&
                r.Name.ToUpper() == normalizedName &&
                r.DeletedAt == null,
                cancellationToken);
    }

    public async Task<PaginatedResponse<RoleListItemResponse>> GetPagedAsync(
        RoleFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = context.Roles
            .AsNoTracking()
            .Include(r => r.UserRoles.Where(ur => ur.Status == UserRoleStatus.Active))
            .AsQueryable();

        // Keyword search on name and description
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var term = filter.Keyword.Trim().ToLowerInvariant();
            query = query.Where(r =>
                EF.Functions.Like(r.Name.ToLower(), $"%{term}%") ||
                (r.Description != null && EF.Functions.Like(r.Description.ToLower(), $"%{term}%")));
        }

        // Filter by active status
        if (filter.IsActive.HasValue)
        {
            var targetStatus = filter.IsActive.Value ? RoleStatus.Active : RoleStatus.Inactive;
            query = query.Where(r => r.Status == targetStatus);
        }

        // Filter by system role
        if (filter.IsSystemRole.HasValue)
            query = query.Where(r => r.IsSystem == filter.IsSystemRole.Value);

        // Only non-deleted roles
        query = query.Where(r => r.DeletedAt == null);

        // Count total
        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = ApplySorting(query, filter.SortBy, filter.SortDirection);

        // Pagination
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize > 100 ? 100 : filter.PageSize;

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleListItemResponse
            {
                Id = r.Id,
                Code = r.Code,
                Name = r.Name,
                Description = r.Description,
                IsSystemRole = r.IsSystem,
                IsActive = r.Status == RoleStatus.Active,
                UserCount = r.UserRoles.Count,
                CreatedAt = r.EffDate,
                UpdatedAt = r.DateLastMaint
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<RoleListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public async Task<Role> AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await context.Roles.AddAsync(role, cancellationToken);
        return role;
    }

    public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        context.Roles.Update(role);
        return Task.CompletedTask;
    }

    public async Task SoftDeleteAsync(Role role, long deletedByUserId, CancellationToken cancellationToken = default)
    {
        role.DeletedAt = DateTimeOffset.UtcNow;
        role.DeletedBy = deletedByUserId;
        role.Status = RoleStatus.Inactive;
        context.Roles.Update(role);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasAssignedUsersAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur =>
                ur.RoleId == roleId &&
                ur.Status == UserRoleStatus.Active &&
                ur.DeletedAt == null,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    private static IQueryable<Role> ApplySorting(IQueryable<Role> query, string? sortBy, string? sortDirection)
    {
        var field = (sortBy ?? "createdAt").ToLowerInvariant();
        var direction = sortDirection?.ToLowerInvariant() == "asc"
            ? SortDirection.Asc
            : SortDirection.Desc;

        if (!AllowedSortFields.Contains(field))
            field = "createdAt";

        query = field switch
        {
            "name" => direction == SortDirection.Asc
                ? query.OrderBy(r => r.Name)
                : query.OrderByDescending(r => r.Name),
            "updatedAt" => direction == SortDirection.Asc
                ? query.OrderBy(r => r.DateLastMaint)
                : query.OrderByDescending(r => r.DateLastMaint),
            _ => direction == SortDirection.Asc
                ? query.OrderBy(r => r.EffDate)
                : query.OrderByDescending(r => r.EffDate)
        };

        return query;
    }

    private async Task<long?> GetCreatorUserIdAsync(Role role, CancellationToken cancellationToken)
    {
        // Infer creator from the first active UserRole assignment.
        var firstAssignment = await context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.RoleId == role.Id && ur.AssignedBy != null)
            .OrderBy(ur => ur.EffDate)
            .Select(ur => (long?)ur.AssignedBy)
            .FirstOrDefaultAsync(cancellationToken);

        return firstAssignment;
    }
}
