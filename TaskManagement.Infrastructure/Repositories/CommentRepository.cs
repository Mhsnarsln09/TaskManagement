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
            // Oldest first so a thread reads top to bottom; Id breaks ties between
            // comments written in the same tick, keeping pagination stable.
            .OrderBy(comment => comment.CreatedAtUtc)
            .ThenBy(comment => comment.Id)
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
