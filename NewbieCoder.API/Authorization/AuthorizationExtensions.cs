using Microsoft.AspNetCore.Authorization;
using NewbieCoder.API.Attributes;
using NewbieCoder.Core.Constants;

namespace NewbieCoder.API.Authorization;

/// <summary>
/// Registers role-based authorization policies at startup.
/// Call <c>AddApiAuthorization()</c> after <c>AddAuthorization()</c> in DI setup.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Registers all authorization policies used by <c>[RequiresRole]</c>.
    /// </summary>
    public static AuthorizationOptions AddApiAuthorization(this AuthorizationOptions options)
    {
        options.AddPolicy(PolicyNames.RequireAuthenticated, policy =>
            policy.RequireAuthenticatedUser());

        options.AddPolicy(PolicyNames.RequireAdmin, policy =>
            policy.RequireRole(RoleConstants.Admin));

        options.AddPolicy(PolicyNames.RequireModerator, policy =>
            policy.RequireRole(RoleConstants.Admin, RoleConstants.Moderator));

        return options;
    }
}

/// <summary>
/// Well-known policy names used throughout the application.
/// </summary>
public static class PolicyNames
{
    public const string RequireAuthenticated = "require-authenticated";
    public const string RequireAdmin        = "require-admin";
    public const string RequireModerator    = "require-moderator";
}
