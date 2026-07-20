using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Comments;

public sealed class Comment : Entity
{
    public Comment(Guid id, Guid taskItemId, Guid authorUserId, string content)
        : base(id)
    {
        if (taskItemId == Guid.Empty)
        {
            throw new ArgumentException("Task id cannot be empty.", nameof(taskItemId));
        }

        if (authorUserId == Guid.Empty)
        {
            throw new ArgumentException("Author user id cannot be empty.", nameof(authorUserId));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Comment content is required.", nameof(content));
        }

        TaskItemId = taskItemId;
        AuthorUserId = authorUserId;
        Content = content.Trim();
    }

    public Guid TaskItemId { get; }

    public Guid AuthorUserId { get; }

    public string Content { get; }
}
