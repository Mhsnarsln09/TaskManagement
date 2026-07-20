using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Attachments;
using TaskManagement.Application.Authentication;
using TaskManagement.Application.Authorization;
using TaskManagement.Application.Comments;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Projects;
using TaskManagement.Application.Statistics;
using TaskManagement.Application.Tasks;
using TaskManagement.Application.Notifications;
using TaskManagement.Application.Validation;
using TaskManagement.Application.Abstractions;

namespace TaskManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateProjectRequest>, CreateProjectRequestValidator>();
        services.AddScoped<IValidator<UpdateProjectRequest>, UpdateProjectRequestValidator>();
        services.AddScoped<IValidator<AddProjectMemberRequest>, AddProjectMemberRequestValidator>();
        services.AddScoped<IValidator<CreateTaskRequest>, CreateTaskRequestValidator>();
        services.AddScoped<IValidator<UpdateTaskRequest>, UpdateTaskRequestValidator>();
        services.AddScoped<IValidator<TaskListQuery>, TaskListQueryValidator>();
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<RefreshTokenRequest>, RefreshTokenRequestValidator>();
        services.AddScoped<IValidator<CreateCommentRequest>, CreateCommentRequestValidator>();
        services.AddScoped<IValidator<CommentListQuery>, CommentListQueryValidator>();
        services.AddScoped<AuthService>();
        services.AddScoped<ProjectAuthorizationService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<TaskService>();
        services.AddScoped<TaskAccessGuard>();
        services.AddScoped<CommentService>();
        services.AddScoped<AttachmentService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<INotificationService>(provider => provider.GetRequiredService<NotificationService>());
        services.AddScoped<DueDateReminderService>();

        // Time-dependent rules (overdue calculation) must not read the system clock
        // directly so tests can substitute a fake TimeProvider.
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
