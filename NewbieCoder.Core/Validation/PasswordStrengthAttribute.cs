using System.ComponentModel.DataAnnotations;

namespace NewbieCoder.Core.Validation;

/// <summary>
/// Validates password strength based on OWASP recommendations:
/// - Length between 6 and 20 characters.
/// - ASCII-only characters (no Unicode/UTF-8 multi-byte sequences).
/// - Not a commonly used password.
/// Returns a single error message regardless of which check failed.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class PasswordStrengthAttribute : ValidationAttribute
{
    public const int MinLength = 6;
    public const int MaxLength = 20;

    public PasswordStrengthAttribute() : base("Password must be between 6 and 20 characters and contain only ASCII characters.") { }

    public override bool IsValid(object? value)
    {
        if (value is not string password)
            return false;

        if (string.IsNullOrWhiteSpace(password))
            return false;

        // Length check.
        if (password.Length < MinLength || password.Length > MaxLength)
            return false;

        // Unicode / non-ASCII check — prevents BCrypt UTF-8 byte-width mismatch.
        // ASCII printable range: 0x20 (space) to 0x7E (tilde).
        // Also allow these safe symbols that BCrypt handles correctly.
        if (!IsAsciiPrintable(password))
            return false;

        // Common password check.
        if (CommonPasswords.Blocklist.Contains(password))
            return false;

        return true;
    }

    /// <summary>
    /// Returns true when every character is a printable ASCII character (0x20–0x7E).
    /// This safely rejects Unicode letters (e.g. ê, â, ô) and C0/C1 control codes.
    /// </summary>
    private static bool IsAsciiPrintable(string s)
    {
        foreach (var c in s)
        {
            if (c < 0x20 || c > 0x7E)
                return false;
        }
        return true;
    }

    public override string FormatErrorMessage(string name)
        => ErrorMessage ?? "Password must be between 6 and 20 characters and contain only ASCII characters.";
}
