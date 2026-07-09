using NewbieCoder.Core.DTOs.Request.Admin;
using NewbieCoder.Core.DTOs.Response.Admin;

namespace NewbieCoder.Core.Interfaces.Services;

public interface IUserService
{
    /// <summary>
    /// Creates a new user account with the specified role.
    /// Admin role can only be assigned through role promotion, not during creation.
    /// </summary>
    /// <param name="request">User creation data.</param>
    /// <param name="createdByUserId">ID of the admin performing the action.</param>
    /// <param name="ipAddress">Client IP address for audit log.</param>
    /// <param name="userAgent">Client User-Agent for audit log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user data (without password/security fields).</returns>
    Task<CreateUserResponse> CreateUserAsync(
        CreateUserRequest request,
        long createdByUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
