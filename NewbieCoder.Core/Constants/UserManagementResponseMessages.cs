namespace NewbieCoder.Core.Constants;

/// <summary>
/// Human-readable error messages used exclusively by the user management flow.
/// </summary>
public static class UserManagementResponseMessages
{
    public const string UserCreatedSuccess = "User account created successfully.";

    public const string UserAlreadyExists = "A user with this email or username already exists.";
    public const string RoleNotFound = "The specified role does not exist.";

    public const string CannotCreateAdminUser = "Directly creating an admin account is not allowed. Promote an existing user instead.";
    public const string UserCreateFailed = "Failed to create user account. Please try again.";

    public const string FullNameRequired = "Full name is required.";
    public const string FullNameTooLong = "Full name must not exceed 100 characters.";

    public const string UsernameRequired = "Username is required.";
    public const string UsernameTooLong = "Username must not exceed 50 characters.";
    public const string UsernameInvalidFormat = "Username may only contain lowercase letters, numbers, underscores, and hyphens.";

    public const string EmailRequired = "Email is required.";
    public const string EmailAlreadyExists = "This email is already in use.";
    public const string InvalidEmailFormat = "Invalid email format.";

    public const string PasswordRequired = "Password is required.";
    public const string PasswordTooWeak = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.";
    public const string PasswordTooShort = "Password must be at least 8 characters.";

    public const string BioTooLong = "Bio must not exceed 500 characters.";
    public const string DisplayTitleTooLong = "Display title must not exceed 100 characters.";
    public const string InvalidUrlFormat = "Invalid URL format.";
}
