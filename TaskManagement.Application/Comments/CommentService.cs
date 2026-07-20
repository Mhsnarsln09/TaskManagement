using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Tasks;
using TaskManagement.Domain.Comments;

namespace TaskManagement.Application.Comments;

// Comments are read/append only in the MVP (docs/tasks/06-mvp-completion.md): editing
// and deleting are out of scope, so no endpoint exists for them. Adding them later
// means deciding who may edit whose comment and whether history is kept, which is a
// separate decision rather than a missing CRUD verb.
public sealed class CommentService(
    ICommentRepository commentRepository,
    IIdentityService identityService,
    TaskAccessGuard taskAccess,
    ICurrentUser currentUser)
{
    public async Task<CommentResponse> CreateAsync(
        Guid projectId,
        Guid taskId,
        CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        await taskAccess.EnsureTaskAccessibleAsync(projectId, taskId, cancellationToken);

        // The author is always the authenticated caller; a client-supplied author id
        // would be an impersonation vector.
        var comment = new Comment(Guid.NewGuid(), taskId, currentUser.UserId, request.Content);

        await commentRepository.AddAsync(comment, cancellationToken);
        await commentRepository.SaveChangesAsync(cancellationToken);

        IReadOnlyDictionary<Guid, UserSummaryResponse> authors =
            await identityService.GetUserSummariesAsync([comment.AuthorUserId], cancellationToken);

        return Map(
            new CommentListItem(
                comment.Id,
                comment.TaskItemId,
                comment.AuthorUserId,
                comment.Content,
                comment.CreatedAtUtc),
            authors);
    }

    public async Task<PagedResponse<CommentResponse>> ListAsync(
        Guid projectId,
        Guid taskId,
        CommentListQuery query,
        CancellationToken cancellationToken)
    {
        await taskAccess.EnsureTaskAccessibleAsync(projectId, taskId, cancellationToken);

        PagedResponse<CommentListItem> page =
            await commentRepository.ListAsync(taskId, query, cancellationToken);

        // Author profiles live in Identity, not in the comments table, so they are
        // resolved once per page instead of once per row.
        Guid[] authorIds = page.Items
            .Select(comment => comment.AuthorUserId)
            .Distinct()
            .ToArray();

        IReadOnlyDictionary<Guid, UserSummaryResponse> authors =
            await identityService.GetUserSummariesAsync(authorIds, cancellationToken);

        return new PagedResponse<CommentResponse>(
            page.Items.Select(comment => Map(comment, authors)).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    private static CommentResponse Map(
        CommentListItem comment,
        IReadOnlyDictionary<Guid, UserSummaryResponse> authors)
    {
        // A deleted account must not break the thread; the comment stays readable
        // with a placeholder author.
        UserSummaryResponse author = authors.TryGetValue(comment.AuthorUserId, out UserSummaryResponse? found)
            ? found
            : new UserSummaryResponse(comment.AuthorUserId, "unknown", null);

        return new CommentResponse(
            comment.Id,
            comment.TaskItemId,
            comment.Content,
            author,
            comment.CreatedAtUtc);
    }
}
