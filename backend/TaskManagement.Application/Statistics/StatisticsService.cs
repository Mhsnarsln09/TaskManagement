using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Authorization;
using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Statistics;

public sealed class StatisticsService(
    ITaskRepository taskRepository,
    ProjectAuthorizationService projectAuthorization,
    TimeProvider timeProvider,
    IApplicationCache cache)
{
    public async Task<ProjectStatisticsResponse> GetAsync(Guid projectId, CancellationToken cancellationToken)
    {
        // Progress is project data, so it is open to every member rather than to the
        // owner only; non-members get the usual 404.
        await projectAuthorization.EnsureMemberAsync(projectId, cancellationToken);

        string cacheKey = $"project-statistics:{projectId}";
        ProjectStatisticsResponse? cached = await cache.GetAsync<ProjectStatisticsResponse>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        ProjectTaskCounts counts = await taskRepository.GetCountsAsync(projectId, today, cancellationToken);

        var response = new ProjectStatisticsResponse(
            projectId,
            counts.Total,
            counts.Todo,
            counts.InProgress,
            counts.Completed,
            counts.Cancelled,
            counts.Overdue,
            CalculateCompletionPercentage(counts));
        await cache.SetAsync(cacheKey, response, TimeSpan.FromSeconds(30), cancellationToken);
        return response;
    }

    // An empty project is 0% complete rather than an error or NaN. The denominator is
    // every task including cancelled ones, so cancelling work does not silently
    // inflate progress.
    private static decimal CalculateCompletionPercentage(ProjectTaskCounts counts)
    {
        if (counts.Total == 0)
        {
            return 0m;
        }

        return Math.Round(counts.Completed * 100m / counts.Total, 2, MidpointRounding.AwayFromZero);
    }
}
