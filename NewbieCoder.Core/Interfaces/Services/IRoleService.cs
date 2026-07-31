using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.Core.Interfaces.Services;

public interface IRoleService
{
    Task<RoleResponse> CreateRoleAsync(
        CreateRoleRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    Task<PaginatedResponse<RoleListItemResponse>> GetRolesAsync(
        RoleFilterRequest filter,
        CancellationToken cancellationToken = default);

    Task<RoleDetailResponse> GetRoleByIdAsync(
        long roleId,
        CancellationToken cancellationToken = default);

    Task<RoleResponse> UpdateRoleAsync(
        long roleId,
        UpdateRoleRequest request,
        long updatedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    Task DeleteRoleAsync(
        long roleId,
        long deletedByUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);
}
