using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.Entities;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Repositories;

/// <summary>
/// Repository for role data access operations.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Retrieves a role by its ID (excludes soft-deleted roles).
    /// </summary>
    Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves full role details including user count and creator info.
    /// </summary>
    Task<RoleDetailResponse?> GetDetailByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an active role with the given name already exists (case-insensitive).
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an active role with the given name exists, excluding the specified role ID.
    /// Used to detect duplicate names during updates.
    /// </summary>
    Task<bool> ExistsByNameExcludingIdAsync(long id, string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of roles matching the filter criteria.
    /// </summary>
    Task<PaginatedResponse<RoleListItemResponse>> GetPagedAsync(RoleFilterRequest filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new role to the database.
    /// </summary>
    Task<Role> AddAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing role as modified.
    /// </summary>
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a role by setting DeletedAt, DeletedBy, and Status = Inactive.
    /// </summary>
    Task SoftDeleteAsync(Role role, long deletedByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the role has any active user assignments.
    /// </summary>
    Task<bool> HasAssignedUsersAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
