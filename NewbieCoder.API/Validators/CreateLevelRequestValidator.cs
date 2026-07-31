using FluentValidation;
using NewbieCoder.Core.DTOs.Request.Admin;

namespace NewbieCoder.API.Validators;

/// <summary>
/// Validates CreateLevelRequest — enforces Code, Name, Description, and DisplayOrder rules.
/// </summary>
public sealed class CreateLevelRequestValidator : AbstractValidator<CreateLevelRequest>
{
    public CreateLevelRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Level code is required.")
            .MaximumLength(50).WithMessage("Level code must not exceed 50 characters.")
            .Matches(@"^[A-Z][A-Z0-9_]*$")
            .WithMessage("Level code must be uppercase alphanumeric with underscores only (e.g. JUNIOR, MIDDLE_2).");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Level name is required.")
            .MinimumLength(2).WithMessage("Level name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Level name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to zero.");
    }
}
