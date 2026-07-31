using FluentValidation;
using NewbieCoder.Core.DTOs.Request.Admin;

namespace NewbieCoder.API.Validators;

/// <summary>
/// Validates CreateCommunityQuestionRequest — enforces Title, Content, AuthorId, and TagIds rules.
/// </summary>
public sealed class CreateCommunityQuestionRequestValidator : AbstractValidator<CreateCommunityQuestionRequest>
{
    public CreateCommunityQuestionRequestValidator()
    {
        RuleFor(x => x.AuthorId)
            .GreaterThan(0).WithMessage("A valid author ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(10).WithMessage("Title must be at least 10 characters.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MinimumLength(20).WithMessage("Content must be at least 20 characters.")
            .MaximumLength(10000).WithMessage("Content must not exceed 10,000 characters.");

        RuleFor(x => x.TagIds)
            .Must(tagIds => tagIds == null || tagIds.Count <= 10)
            .WithMessage("A question can have at most 10 tags.");
    }
}
