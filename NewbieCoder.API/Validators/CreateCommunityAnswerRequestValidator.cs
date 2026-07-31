using FluentValidation;
using NewbieCoder.Core.DTOs.Request.Admin;

namespace NewbieCoder.API.Validators;

/// <summary>
/// Validates CreateCommunityAnswerRequest — enforces QuestionId, AuthorId, and Content rules.
/// </summary>
public sealed class CreateCommunityAnswerRequestValidator : AbstractValidator<CreateCommunityAnswerRequest>
{
    public CreateCommunityAnswerRequestValidator()
    {
        RuleFor(x => x.QuestionId)
            .GreaterThan(0).WithMessage("A valid question ID is required.");

        RuleFor(x => x.AuthorId)
            .GreaterThan(0).WithMessage("A valid author ID is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Answer content is required.")
            .MinimumLength(20).WithMessage("Answer content must be at least 20 characters.")
            .MaximumLength(10000).WithMessage("Answer content must not exceed 10,000 characters.");
    }
}
