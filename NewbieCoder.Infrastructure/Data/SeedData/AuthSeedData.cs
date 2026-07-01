using NewbieCoder.Core.Constants;

namespace NewbieCoder.Infrastructure.Data.SeedData;

/// <summary>
/// Static seed data for roles.
/// All reference data lives here so AuthDbSeeder stays focused on persistence logic.
/// </summary>
public static class AuthSeedData
{
    public static readonly Dictionary<string, string> RoleDisplayNames = new()
    {
        [RoleConstants.Admin]     = "Administrator",
        [RoleConstants.Moderator] = "Moderator",
        [RoleConstants.Author]    = "Author",
        [RoleConstants.User]      = "User",
        [RoleConstants.Guest]     = "Guest",
    };

    public static readonly Dictionary<string, string?> RoleDescriptions = new()
    {
        [RoleConstants.Admin]     = "Full system access. Manages users, roles, content, and system configuration.",
        [RoleConstants.Moderator] = "Moderates community content, approves/rejects posts, and manages tags.",
        [RoleConstants.Author]    = "Can create and publish blog posts, interview questions, and series.",
        [RoleConstants.User]      = "Standard authenticated user. Can bookmark, vote, comment, and ask community Q&A.",
        [RoleConstants.Guest]     = "Unauthenticated visitor. Can read public content and search.",
    };
}
