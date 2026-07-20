namespace TaskManagement.Application.Contracts;

public sealed record CreateCommentRequest(string Content);

public sealed record CommentListQuery(int Page = 1, int PageSize = 20);

// Repository-facing projection: data access knows the author id but not the author
// profile, which is owned by Identity. CommentService joins the two.
public sealed record CommentListItem(
    Guid Id,
    Guid TaskItemId,
    Guid AuthorUserId,
    string Content,
    DateTimeOffset CreatedAtUtc);

public sealed record CommentResponse(
    Guid Id,
    Guid TaskItemId,
    string Content,
    UserSummaryResponse Author,
    DateTimeOffset CreatedAtUtc);
