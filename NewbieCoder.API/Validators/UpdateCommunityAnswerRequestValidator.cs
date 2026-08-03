using FluentValidation;
using NewbieCoder.Core.DTOs.Request.Admin;

namespace NewbieCoder.API.Validators;

/// <summary>
/// Validates UpdateCommunityAnswerRequest — enforces Content rules.
/// </summary>
public sealed class UpdateCommunityAnswerRequestValidator : AbstractValidator<UpdateCommunityAnswerRequest>
{
    public UpdateCommunityAnswerRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Answer content is required.")
            .MinimumLength(20).WithMessage("Answer content must be at least 20 characters.")
            .MaximumLength(10000).WithMessage("Answer content must not exceed 10,000 characters.");
    }
}
