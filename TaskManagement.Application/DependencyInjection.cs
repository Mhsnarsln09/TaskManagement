using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Authentication;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Projects;
using TaskManagement.Application.Tasks;
using TaskManagement.Application.Validation;

namespace TaskManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateProjectRequest>, CreateProjectRequestValidator>();
        services.AddScoped<IValidator<UpdateProjectRequest>, UpdateProjectRequestValidator>();
        services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
        services.AddScoped<IValidator<UpdateTaskRequest>, UpdateTaskRequestValidator>();
        services.AddScoped<IValidator<TaskListQuery>, TaskListQueryValidator>();
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<AuthService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<TaskService>();

        return services;
    }
}
