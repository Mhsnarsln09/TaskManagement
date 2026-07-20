using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Comments;

namespace TaskManagement.Application.Abstractions;

public interface ICommentRepository
{
    Task AddAsync(Comment comment, CancellationToken cancellationToken);

    Task<PagedResponse<CommentListItem>> ListAsync(
        Guid taskItemId,
        CommentListQuery query,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
