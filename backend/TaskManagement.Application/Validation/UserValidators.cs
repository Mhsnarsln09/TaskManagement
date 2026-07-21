using FluentValidation;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Users;

namespace TaskManagement.Application.Validation;

public sealed class UserSearchQueryValidator : AbstractValidator<UserSearchQuery>
{
    public UserSearchQueryValidator()
    {
        RuleFor(query => query.Search)
            .NotEmpty()
            .MinimumLength(UserDirectoryService.MinimumSearchLength)
            .WithMessage(
                $"search must be at least {UserDirectoryService.MinimumSearchLength} characters.")
            .MaximumLength(256);

        RuleFor(query => query.Limit)
            .InclusiveBetween(1, UserDirectoryService.MaximumLimit);
    }
}
