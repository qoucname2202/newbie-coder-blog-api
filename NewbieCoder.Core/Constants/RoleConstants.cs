namespace NewbieCoder.Core.Constants;

/// <summary>
/// System role codes. Each code is unique and immutable — system roles must never be deleted
/// from the database (IsSystem = true guards deletion in business logic).
/// </summary>
public static class RoleConstants
{
    public const string Admin       = "ADMIN";
    public const string Moderator   = "MODERATOR";
    public const string Author      = "AUTHOR";
    public const string User        = "USER";
    public const string Guest       = "GUEST";

    /// <summary>
    /// All system role codes in ascending privilege order.
    /// </summary>
    public static readonly string[] AllCodes =
    [
        Guest,
        User,
        Author,
        Moderator,
        Admin
    ];
}
