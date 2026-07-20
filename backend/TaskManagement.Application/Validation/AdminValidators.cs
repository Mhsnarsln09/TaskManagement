using FluentValidation;
using TaskManagement.Application.Authentication;
using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Validation;

public sealed class AdminUserListQueryValidator : AbstractValidator<AdminUserListQuery>
{
    public AdminUserListQueryValidator()
    {
        this.AddPaginationRules(query => query.Page, query => query.PageSize);
        RuleFor(query => query.Search).MaximumLength(256);
    }
}

public sealed class ReplaceUserRolesRequestValidator : AbstractValidator<ReplaceUserRolesRequest>
{
    public ReplaceUserRolesRequestValidator()
    {
        RuleFor(request => request.Roles)
            .NotNull()
            .Must(roles => roles.Count > 0)
            .WithMessage("At least one role is required.");

        RuleForEach(request => request.Roles)
            .Must(role => ApplicationRoles.All.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be one of: " + string.Join(", ", ApplicationRoles.All) + ".");
    }
}
