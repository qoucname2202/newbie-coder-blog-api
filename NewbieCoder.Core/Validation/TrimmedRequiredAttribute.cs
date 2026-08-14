using System.ComponentModel.DataAnnotations;

namespace NewbieCoder.Core.Validation;

/// <summary>
/// Validates that a string value is not null, empty, or whitespace-only.
/// Differs from [Required] which only checks null/empty, allowing whitespace-only
/// strings to pass through. Trims the value before checking.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TrimmedRequiredAttribute : ValidationAttribute
{
    public TrimmedRequiredAttribute() : base("This field is required.") { }

    public TrimmedRequiredAttribute(string errorMessage) : base(errorMessage) { }

    public override bool IsValid(object? value)
    {
        if (value is not string raw)
            return false;

        return !string.IsNullOrWhiteSpace(raw);
    }

    public override string FormatErrorMessage(string name)
        => ErrorMessage ?? "This field is required.";
}
