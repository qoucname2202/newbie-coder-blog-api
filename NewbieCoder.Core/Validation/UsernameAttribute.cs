using System.ComponentModel.DataAnnotations;

namespace NewbieCoder.Core.Validation;

/// <summary>
/// Validates a username by first trimming leading/trailing whitespace.
/// Fails if:
/// - The trimmed value is empty (blank input).
/// - Length is less than 3 or greater than 30 characters.
/// - Contains any non-lowercase letters, digits, underscores, or hyphens.
/// - Does not contain at least one lowercase letter (a-z).
/// Always returns a single error message regardless of which check failed.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class UsernameAttribute : ValidationAttribute
{
    public const int MinLength = 3;
    public const int MaxLength = 30;

    // Allowed: a-z 0-9 _ -
    private static readonly char[] AllowedChars = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
        'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '_', '-'];

    public UsernameAttribute() : base("Username invalid format.") { }

    public override bool IsValid(object? value)
    {
        if (value is not string raw)
            return false;

        var trimmed = raw.Trim();

        // Blank after trim → invalid.
        if (string.IsNullOrEmpty(trimmed))
            return false;

        // Length check.
        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
            return false;

        // Character format check — every char must be allowed.
        foreach (var c in trimmed)
        {
            if (c < 'a' || c > 'z')
            {
                // Digit, underscore, or hyphen is allowed.
                if (c == '_' || c == '-' || (c >= '0' && c <= '9'))
                    continue;
                return false;
            }
        }

        // Must contain at least one lowercase letter.
        if (!trimmed.Any(char.IsLetter))
            return false;

        return true;
    }

    public override string FormatErrorMessage(string name)
        => "Username invalid format.";
}
