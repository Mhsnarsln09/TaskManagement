using NSubstitute;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Authorization;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;
using TaskManagement.Application.Tasks;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.UnitTests.Application;

// Exercises the TaskService business decisions with mocked repositories: assignee
// membership validation, the completed-task reopen policy and the allowed status
// transitions. The HTTP status mapping of the thrown exceptions is covered by the
// integration tests.
public sealed class TaskServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 07, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IApplicationCache _cache = Substitute.For<IApplicationCache>();
    private readonly TaskService _service;
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly Guid _callerId = Guid.NewGuid();

    public TaskServiceTests()
    {
        _currentUser.UserId.Returns(_callerId);
        GrantMembership(_callerId);

        _service = new TaskService(
            _projectRepository,
            _taskRepository,
            new ProjectAuthorizationService(_projectRepository, _currentUser),
            new FixedTimeProvider(Now),
            _notificationService,
            _cache);
    }

    [Fact]
    public async Task CreateTask_ValidRequest_PersistsTaskAndReturnsResponse()
    {
        TaskResponse response = await _service.CreateAsync(
            _projectId,
            CreateRequest(title: "Write tests"),
            CancellationToken.None);

        Assert.Equal("Write tests", response.Title);
        Assert.Equal(WorkItemStatus.Todo, response.Status);
        await _taskRepository.Received(1).AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        await _taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTask_AssigneeIsNotProjectMember_ThrowsValidationProblem()
    {
        Guid outsiderId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<ValidationProblemException>(
            () => _service.CreateAsync(
                _projectId,
                CreateRequest(assigneeUserId: outsiderId),
                CancellationToken.None));

        Assert.Contains("assigneeUserId", exception.Errors.Keys);
        await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTask_AssigneeIsProjectMember_Passes()
    {
        Guid assigneeId = Guid.NewGuid();
        GrantMembership(assigneeId);

        TaskResponse response = await _service.CreateAsync(
            _projectId,
            CreateRequest(assigneeUserId: assigneeId),
            CancellationToken.None);

        Assert.Equal(assigneeId, response.AssigneeUserId);
    }

    [Fact]
    public async Task CreateTask_CallerIsNotProjectMember_ThrowsNotFound()
    {
        Guid foreignProjectId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(foreignProjectId, CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTask_TaskIsCompletedWithoutReopen_RejectsEdit()
    {
        SetExistingTask(CompletedTask());

        await Assert.ThrowsAsync<DomainException>(
            () => _service.UpdateAsync(
                _projectId,
                Guid.NewGuid(),
                UpdateRequest(title: "New title", status: WorkItemStatus.Completed),
                CancellationToken.None));

        await _taskRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTask_ReopeningCompletedTask_AllowsEditing()
    {
        SetExistingTask(CompletedTask());

        TaskResponse response = await _service.UpdateAsync(
            _projectId,
            Guid.NewGuid(),
            UpdateRequest(title: "Edited after reopen", status: WorkItemStatus.InProgress),
            CancellationToken.None);

        Assert.Equal(WorkItemStatus.InProgress, response.Status);
        Assert.Equal("Edited after reopen", response.Title);
        await _taskRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_BackToTodo_RejectsInvalidTransition()
    {
        SetExistingTask(InProgressTask());

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateAsync(
                _projectId,
                Guid.NewGuid(),
                UpdateRequest(status: WorkItemStatus.Todo),
                CancellationToken.None));
    }

    [Fact]
    public async Task ChangeStatus_TodoToCompleted_RunsStartAndComplete()
    {
        SetExistingTask(TodoTask());

        TaskResponse response = await _service.UpdateAsync(
            _projectId,
            Guid.NewGuid(),
            UpdateRequest(status: WorkItemStatus.Completed),
            CancellationToken.None);

        Assert.Equal(WorkItemStatus.Completed, response.Status);
    }

    [Fact]
    public async Task ChangeStatus_CancelledTask_RejectsFurtherTransitions()
    {
        TaskItem task = TodoTask();
        task.Cancel();
        SetExistingTask(task);

        await Assert.ThrowsAsync<DomainException>(
            () => _service.UpdateAsync(
                _projectId,
                Guid.NewGuid(),
                UpdateRequest(status: WorkItemStatus.InProgress),
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdateTask_TaskDoesNotExist_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateAsync(
                _projectId,
                Guid.NewGuid(),
                UpdateRequest(),
                CancellationToken.None));
    }

    [Fact]
    public async Task DeleteTask_UserIsMemberButNotOwner_ThrowsForbidden()
    {
        _projectRepository.GetOwnerIdAsync(_projectId, Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.DeleteAsync(_projectId, Guid.NewGuid(), CancellationToken.None));

        _taskRepository.DidNotReceive().Remove(Arg.Any<TaskItem>());
    }

    private TaskItem TodoTask()
    {
        return new TaskItem(
            Guid.NewGuid(),
            _projectId,
            "Task",
            null,
            TaskPriority.Medium,
            null,
            null);
    }

    private TaskItem InProgressTask()
    {
        TaskItem task = TodoTask();
        task.Start();
        return task;
    }

    private TaskItem CompletedTask()
    {
        TaskItem task = InProgressTask();
        task.Complete();
        return task;
    }

    private void GrantMembership(Guid userId)
    {
        _projectRepository.IsMemberAsync(_projectId, userId, Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private void SetExistingTask(TaskItem task)
    {
        _taskRepository.GetEntityAsync(_projectId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(task);
    }

    private static CreateTaskRequest CreateRequest(
        string title = "Task",
        Guid? assigneeUserId = null)
    {
        return new CreateTaskRequest(title, null, TaskPriority.Medium, null, assigneeUserId);
    }

    private static UpdateTaskRequest UpdateRequest(
        string title = "Task",
        WorkItemStatus status = WorkItemStatus.InProgress)
    {
        return new UpdateTaskRequest(title, null, status, TaskPriority.Medium, null, null);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
