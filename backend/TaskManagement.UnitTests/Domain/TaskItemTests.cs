using TaskManagement.Domain.Common;
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

        Assert.Throws<DomainException>(task.Complete);
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

        Assert.Throws<DomainException>(() => task.Rename("New title"));
    }

    [Fact]
    public void Start_RejectsInProgressTask()
    {
        TaskItem task = CreateTask();
        task.Start();

        Assert.Throws<DomainException>(task.Start);
    }

    [Fact]
    public void Cancel_AllowsTodoAndInProgressTasks()
    {
        TaskItem todoTask = CreateTask();
        todoTask.Cancel();
        Assert.Equal(WorkItemStatus.Cancelled, todoTask.Status);

        TaskItem inProgressTask = CreateTask();
        inProgressTask.Start();
        inProgressTask.Cancel();
        Assert.Equal(WorkItemStatus.Cancelled, inProgressTask.Status);
    }

    [Fact]
    public void Cancel_RejectsCompletedTask()
    {
        TaskItem task = CreateTask();
        task.Start();
        task.Complete();

        Assert.Throws<DomainException>(task.Cancel);
    }

    [Fact]
    public void Reopen_MovesCompletedTaskBackToInProgress()
    {
        TaskItem task = CreateTask();
        task.Start();
        task.Complete();

        task.Reopen();

        Assert.Equal(WorkItemStatus.InProgress, task.Status);
    }

    [Fact]
    public void Reopen_AllowsEditingAgain()
    {
        TaskItem task = CreateTask();
        task.Start();
        task.Complete();
        task.Reopen();

        task.Rename("New title");

        Assert.Equal("New title", task.Title);
    }

    [Fact]
    public void Reopen_RejectsTaskThatIsNotCompleted()
    {
        TaskItem todoTask = CreateTask();
        Assert.Throws<DomainException>(todoTask.Reopen);

        TaskItem cancelledTask = CreateTask();
        cancelledTask.Cancel();
        Assert.Throws<DomainException>(cancelledTask.Reopen);
    }

    [Fact]
    public void Rename_RejectsCancelledTask()
    {
        TaskItem task = CreateTask();
        task.Cancel();

        Assert.Throws<DomainException>(() => task.Rename("New title"));
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
