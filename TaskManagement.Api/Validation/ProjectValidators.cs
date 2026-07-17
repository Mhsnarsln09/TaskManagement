using FluentValidation;
using TaskManagement.Api.Contracts;

namespace TaskManagement.Api.Validation;

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(request => request.Description)
            .MaximumLength(2_000);
    }
}

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(160);

        RuleFor(request => request.Description)
            .MaximumLength(2_000);
    }
}
