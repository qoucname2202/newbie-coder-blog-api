namespace NewbieCoder.API.Attributes;

/// <summary>
/// Specifies the minimum role required to access a controller or action.
/// Usage: <c>[RequiresRole(RoleConstants.Admin)]</c>
///        <c>[RequiresRole(RoleConstants.Admin, RoleConstants.Moderator)]</c> (any of)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequiresRoleAttribute : Attribute
{
    public IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Requires any one of the specified roles (OR logic).
    /// </summary>
    public RequiresRoleAttribute(params string[] roles)
    {
        Roles = roles.ToList().AsReadOnly();
    }
}
