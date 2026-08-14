using System.ComponentModel.DataAnnotations;

namespace NewbieCoder.Core.Validation;

/// <summary>
/// Validates an email string by first trimming leading/trailing whitespace.
/// Fails if:
/// - The trimmed value is empty (blank input).
/// - The local part (before @) exceeds 64 characters (RFC 5321).
/// - The total email exceeds 255 characters.
/// - Contains embedded whitespace.
/// - Contains any non-ASCII characters (Unicode, emoji, CJK, accented chars, etc.).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TrimmedEmailAttribute : ValidationAttribute
{
    private const int MaxLocalPartLength = 64;   // RFC 5321 §4.5.3.1
    private const int MaxTotalEmailLength = 255; // Domain name limit

    // Reuse the same constant string used throughout the registration flow.
    // Using a static reference avoids hardcoding the literal string in two places.
    public TrimmedEmailAttribute() : base("Invalid email format.") { }

    public override bool IsValid(object? value)
    {
        if (value is not string raw)
            return true;

        var trimmed = raw.Trim();

        // Blank after trim → treat as missing.
        if (string.IsNullOrEmpty(trimmed))
            return false;

        // Embedded whitespace anywhere → invalid.
        if (trimmed.Any(char.IsWhiteSpace))
            return false;

        // Non-ASCII character present → invalid (blocks Unicode, emoji, CJK, accented chars).
        if (trimmed.Any(c => c < 32 || c > 126))
            return false;

        // Total length check.
        if (trimmed.Length > MaxTotalEmailLength)
            return false;

        // Split and validate local part (before @).
        var atIndex = trimmed.LastIndexOf('@');
        if (atIndex < 0)
            return false; // No @ → not a valid email structure.

        var localPart = trimmed[..atIndex];
        if (localPart.Length > MaxLocalPartLength)
            return false;

        return true;
    }

    public override string FormatErrorMessage(string name)
        => "Invalid email format.";
}
