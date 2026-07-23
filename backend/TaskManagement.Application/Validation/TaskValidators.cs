using FluentValidation;
using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Validation;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .MaximumLength(4_000);

        RuleFor(request => request.Priority)
            .IsInEnum();

        RuleFor(request => request.DueDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .When(request => request.DueDate is not null);

        RuleFor(request => request.AssigneeUserId)
            .NotEqual(Guid.Empty)
            .When(request => request.AssigneeUserId is not null);
    }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Description)
            .MaximumLength(4_000);

        RuleFor(request => request.Status)
            .IsInEnum();

        RuleFor(request => request.Priority)
            .IsInEnum();

        // The past-due-date rule is intentionally NOT enforced here (B10-05): a
        // stateless validator cannot tell whether the due date is actually changing,
        // so it would wrongly block editing the other fields of an already-overdue
        // task. TaskService.UpdateAsync rejects a past due date only when it changes.

        RuleFor(request => request.AssigneeUserId)
            .NotEqual(Guid.Empty)
            .When(request => request.AssigneeUserId is not null);
    }
}

public sealed class TaskListQueryValidator : AbstractValidator<TaskListQuery>
{
    private static readonly string[] SortFields =
    [
        "title",
        "status",
        "priority",
        "dueDate",
        "createdAtUtc"
    ];

    public TaskListQueryValidator()
    {
        this.AddPaginationRules(query => query.Page, query => query.PageSize);

        RuleFor(query => query.Status)
            .IsInEnum()
            .When(query => query.Status is not null);

        RuleFor(query => query.Priority)
            .IsInEnum()
            .When(query => query.Priority is not null);

        RuleFor(query => query.SortBy)
            .Must(sortBy => sortBy is null || SortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"sortBy must be one of: {string.Join(", ", SortFields)}.");

        RuleFor(query => query.SortDirection)
            .Must(direction => direction is null
                || direction.Equals("asc", StringComparison.OrdinalIgnoreCase)
                || direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("sortDirection must be either asc or desc.");
    }
}
