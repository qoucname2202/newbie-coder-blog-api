using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NewbieCoder.API.Extensions;

/// <summary>
/// Extension methods for extracting authentication claims from a ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the current user ID from the 'sub' claim, or null if not authenticated.
    /// </summary>
    public static long? GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier);

        return sub != null && long.TryParse(sub.Value, out var id) ? id : null;
    }

    /// <summary>
    /// Returns the session ID from the 'session_id' claim, or null if not present.
    /// </summary>
    public static long? GetSessionId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("session_id");
        return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
    }

    /// <summary>
    /// Returns the device ID from the 'device_id' claim, or null if not present.
    /// </summary>
    public static long? GetDeviceId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst("device_id");
        return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
    }

    /// <summary>
    /// Returns all role codes from the Role claim type.
    /// </summary>
    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList()
            .AsReadOnly();
    }
}
