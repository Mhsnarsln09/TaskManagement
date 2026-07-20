using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Tasks;

public sealed class TaskItem : SoftDeletableEntity
{
    public TaskItem(
        Guid id,
        Guid projectId,
        string title,
        string? description,
        TaskPriority priority,
        DateOnly? dueDate,
        Guid? assigneeUserId)
        : base(id)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title is required.", nameof(title));
        }

        ProjectId = projectId;
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Priority = priority;
        DueDate = dueDate;
        AssigneeUserId = assigneeUserId;
        Status = WorkItemStatus.Todo;
    }

    public Guid ProjectId { get; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public WorkItemStatus Status { get; private set; }

    public TaskPriority Priority { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public Guid? AssigneeUserId { get; private set; }

    public bool IsOverdue(DateOnly today)
    {
        return DueDate is not null
            && DueDate < today
            && Status != WorkItemStatus.Completed
            && Status != WorkItemStatus.Cancelled;
    }

    public void Start()
    {
        EnsureCanChange();

        if (Status != WorkItemStatus.Todo)
        {
            throw new DomainException("Only todo tasks can be started.");
        }

        Status = WorkItemStatus.InProgress;
        MarkUpdated();
    }

    public void Complete()
    {
        EnsureCanChange();

        if (Status != WorkItemStatus.InProgress)
        {
            throw new DomainException("Only in progress tasks can be completed.");
        }

        Status = WorkItemStatus.Completed;
        MarkUpdated();
    }

    public void Cancel()
    {
        EnsureCanChange();

        Status = WorkItemStatus.Cancelled;
        MarkUpdated();
    }

    public void Reopen()
    {
        if (Status != WorkItemStatus.Completed)
        {
            throw new DomainException("Only completed tasks can be reopened.");
        }

        Status = WorkItemStatus.InProgress;
        MarkUpdated();
    }

    public void Rename(string title)
    {
        EnsureCanChange();

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title is required.", nameof(title));
        }

        Title = title.Trim();
        MarkUpdated();
    }

    public void ChangeDescription(string? description)
    {
        EnsureCanChange();

        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MarkUpdated();
    }

    public void ChangePriority(TaskPriority priority)
    {
        EnsureCanChange();

        Priority = priority;
        MarkUpdated();
    }

    public void ChangeDueDate(DateOnly? dueDate)
    {
        EnsureCanChange();

        DueDate = dueDate;
        MarkUpdated();
    }

    public void AssignTo(Guid? userId)
    {
        EnsureCanChange();

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        AssigneeUserId = userId;
        MarkUpdated();
    }

    private void EnsureCanChange()
    {
        if (Status == WorkItemStatus.Completed)
        {
            throw new DomainException("Completed tasks cannot be changed.");
        }

        if (Status == WorkItemStatus.Cancelled)
        {
            throw new DomainException("Cancelled tasks cannot be changed.");
        }
    }
}
