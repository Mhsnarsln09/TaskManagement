using FluentValidation;
using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Validation;

public sealed class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        // The 2.000 character ceiling mirrors CommentConfiguration so a request that
        // passes validation cannot be rejected later by the database.
        RuleFor(request => request.Content)
            .NotEmpty()
            .MaximumLength(2_000);
    }
}

public sealed class CommentListQueryValidator : AbstractValidator<CommentListQuery>
{
    public CommentListQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}
