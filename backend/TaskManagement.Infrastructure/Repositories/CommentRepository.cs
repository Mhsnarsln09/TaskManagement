using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Comments;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class CommentRepository(ApplicationDbContext dbContext) : ICommentRepository
{
    public async Task AddAsync(Comment comment, CancellationToken cancellationToken)
    {
        await dbContext.Comments.AddAsync(comment, cancellationToken);
    }

    public async Task<PagedResponse<CommentListItem>> ListAsync(
        Guid taskItemId,
        CommentListQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<Comment> comments = dbContext.Comments
            .AsNoTracking()
            .Where(comment => comment.TaskItemId == taskItemId);

        int totalCount = await comments.CountAsync(cancellationToken);

        List<CommentListItem> items = await comments
            // Newest first (B10-04): the first page always carries the most recent
            // comments so a busy thread shows current activity without paging to the
            // end. Id breaks ties between comments written in the same tick — descending
            // as well — so the ordering is total and pagination never skips or repeats.
            .OrderByDescending(comment => comment.CreatedAtUtc)
            .ThenByDescending(comment => comment.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(comment => new CommentListItem(
                comment.Id,
                comment.TaskItemId,
                comment.AuthorUserId,
                comment.Content,
                comment.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResponse<CommentListItem>(items, query.Page, query.PageSize, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
