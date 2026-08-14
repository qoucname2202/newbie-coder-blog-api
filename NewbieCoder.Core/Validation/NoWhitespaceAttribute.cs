using System.ComponentModel.DataAnnotations;

namespace NewbieCoder.Core.Validation;

/// <summary>
/// Validates that a string value does not contain any whitespace characters (space, tab, etc.).
/// Use this for fields like email where whitespace is never valid input.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class NoWhitespaceAttribute : ValidationAttribute
{
    public NoWhitespaceAttribute() : base("The field must not contain any whitespace characters.") { }

    public override bool IsValid(object? value)
    {
        if (value is not string str)
            return true; // Let other validators handle non-string values.

        return !str.Any(char.IsWhiteSpace);
    }

    public override string FormatErrorMessage(string name)
        => $"'{name}' must not contain any whitespace characters.";
}
