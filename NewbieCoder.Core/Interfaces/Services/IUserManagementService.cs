using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;

namespace NewbieCoder.Core.Interfaces.Services;

public interface IUserManagementService
{
    Task<LockUserResponse> LockUserAsync(
        long targetUserId,
        LockUserRequest request,
        long adminUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);

    Task<LockedUserDto> UnlockUserAsync(
        long targetUserId,
        long adminUserId,
        string? ipAddress,
        string? userAgent,
        string? traceId,
        CancellationToken cancellationToken = default);
}
