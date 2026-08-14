namespace NewbieCoder.Core.Constants;

/// <summary>
/// Human-readable error messages used exclusively by the registration flow.
/// </summary>
public static class RegisterResponseMessages
{
    public const string EmailRequired = "Email is required.";
    public const string EmailAlreadyExists = "This email is already in use.";
    public const string InvalidEmailFormat = "Invalid email format.";

    public const string UsernameRequired = "Username is required.";
    public const string UsernameAlreadyExists = "This username is already taken.";
    public const string UsernameTooShort = "Username must be at least 3 characters.";
    public const string UsernameTooLong = "Username must not exceed 100 characters.";
    public const string UsernameInvalidFormat = "Username may only contain lowercase letters, numbers, underscores, and hyphens.";

    public const string PasswordRequired = "Password is required.";
    public const string PasswordTooShort = "Password must be between 6 and 20 characters.";
    public const string PasswordTooLong = "Password must be between 6 and 20 characters.";
    public const string PasswordTooWeak = "Password must be between 6 and 20 characters.";
    public const string PasswordContainsWhitespace = "Password must not contain whitespace characters.";
    public const string PasswordContainsUnicode = "Password must contain only ASCII letters, numbers, and special characters. Unicode characters are not allowed.";
    public const string PasswordContainsUserInfo = "Password must not contain your full name or username.";
    public const string PasswordEqualsEmail = "Password must not be the same as your email address.";

    public const string ConfirmPasswordRequired = "Confirm password is required.";
    public const string PasswordNotMatch = "Passwords do not match.";

    public const string FullNameRequired = "Full name is required.";
    public const string FullNameTooShort = "Full name must be at least 2 characters.";
    public const string FullNameTooLong = "Full name must not exceed 150 characters.";
    public const string FullNameHasWhitespace = "Full name must not contain leading or trailing whitespace.";

    public const string TermsNotAccepted = "You must accept the terms of service.";
    public const string TermsRequired = "Accept terms field is required.";

    public const string DeviceBlocked = "This device has been blocked from registration.";

    public const string DefaultRoleNotFound = "Default role not found in the system. Please contact an administrator.";
    public const string InternalError = "An unexpected error occurred during registration.";

    public const string RegisterSuccess = "Registration successful.";
}
