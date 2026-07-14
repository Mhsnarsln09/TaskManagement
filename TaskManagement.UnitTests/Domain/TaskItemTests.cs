using TaskManagement.Domain.Tasks;

namespace TaskManagement.UnitTests.Domain;

public sealed class TaskItemTests
{
    [Fact]
    public void Constructor_RequiresProjectId()
    {
        Assert.Throws<ArgumentException>(() =>
            new TaskItem(
                Guid.NewGuid(),
                Guid.Empty,
                "Task",
                null,
                TaskPriority.Medium,
                null,
                null));
    }

    [Fact]
    public void Complete_RejectsTaskThatWasNotStarted()
    {
        TaskItem task = CreateTask();

        Assert.Throws<InvalidOperationException>(task.Complete);
    }

    [Fact]
    public void Complete_AllowsInProgressTask()
    {
        TaskItem task = CreateTask();

        task.Start();
        task.Complete();

        Assert.Equal(WorkItemStatus.Completed, task.Status);
    }

    [Fact]
    public void Rename_RejectsCompletedTask()
    {
        TaskItem task = CreateTask();
        task.Start();
        task.Complete();

        Assert.Throws<InvalidOperationException>(() => task.Rename("New title"));
    }

    [Fact]
    public void IsOverdue_ReturnsTrue_WhenDueDateIsPastAndTaskIsOpen()
    {
        TaskItem task = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Task",
            null,
            TaskPriority.High,
            new DateOnly(2026, 7, 13),
            null);

        Assert.True(task.IsOverdue(new DateOnly(2026, 7, 14)));
    }

    [Fact]
    public void IsOverdue_ReturnsFalse_WhenTaskIsCompleted()
    {
        TaskItem task = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Task",
            null,
            TaskPriority.High,
            new DateOnly(2026, 7, 13),
            null);

        task.Start();
        task.Complete();

        Assert.False(task.IsOverdue(new DateOnly(2026, 7, 14)));
    }

    private static TaskItem CreateTask()
    {
        return new TaskItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Task",
            null,
            TaskPriority.Medium,
            null,
            null);
    }
}
