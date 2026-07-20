namespace TaskManagement.Application.Contracts;

// Raw counts produced by a single repository projection; the derived completion
// percentage is calculated in StatisticsService so the zero-task case stays a
// business rule rather than a SQL detail.
public sealed record ProjectTaskCounts(
    int Total,
    int Todo,
    int InProgress,
    int Completed,
    int Cancelled,
    int Overdue);

public sealed record ProjectStatisticsResponse(
    Guid ProjectId,
    int TotalTasks,
    int TodoTasks,
    int InProgressTasks,
    int CompletedTasks,
    int CancelledTasks,
    int OverdueTasks,
    decimal CompletionPercentage);
