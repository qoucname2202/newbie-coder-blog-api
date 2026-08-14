using System.ComponentModel.DataAnnotations;

namespace NewbieCoder.Core.Validation;

/// <summary>
/// Validates that a full name:
/// - does not contain leading or trailing whitespace
/// - contains only letters (including Vietnamese diacritics) and spaces
/// - does not contain HTML/JS injection tags
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class FullNameAttribute : ValidationAttribute
{
    private static readonly System.Text.RegularExpressions.Regex HtmlTagPattern =
        new(@"<[^>]+>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public FullNameAttribute() : base("Full name must not contain leading or trailing whitespace or digits.") { }

    public FullNameAttribute(string errorMessage) : base(errorMessage) { }

    public override bool IsValid(object? value)
    {
        if (value is not string raw)
            return false;

        // A null / empty value is handled by [Required] / [TrimmedRequired].
        if (string.IsNullOrEmpty(raw))
            return true;

        // Reject HTML/JS injection payloads (e.g. "<script>alert(1)</script>").
        if (HtmlTagPattern.IsMatch(raw))
            return false;

        // Reject leading or trailing whitespace — inner spaces between words are fine ("John Doe").
        if (raw.Trim().Length != raw.Length)
            return false;

        // Only letters (A-Z, a-z, Vietnamese diacritics) and spaces — no digits allowed.
        // \p{L} matches any Unicode letter; space matches U+0020.
        return System.Text.RegularExpressions.Regex.IsMatch(raw, @"^[\p{L} ]+$");
    }

    public override string FormatErrorMessage(string name)
        => ErrorMessage ?? "Full name must not contain leading or trailing whitespace.";
}
