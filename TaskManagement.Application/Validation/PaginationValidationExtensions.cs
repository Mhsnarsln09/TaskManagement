using FluentValidation;

namespace TaskManagement.Application.Validation;

internal static class PaginationValidationExtensions
{
    public static void AddPaginationRules<T>(
        this AbstractValidator<T> validator,
        Func<T, int> page,
        Func<T, int> pageSize)
    {
        validator.RuleFor(request => page(request)).GreaterThanOrEqualTo(1).WithName("Page");
        validator.RuleFor(request => pageSize(request)).InclusiveBetween(1, 100).WithName("PageSize");
    }
}
