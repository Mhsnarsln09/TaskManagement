using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Attachments;

namespace TaskManagement.Application.Abstractions;

public interface IAttachmentRepository
{
    Task AddAsync(Attachment attachment, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AttachmentListItem>> ListAsync(
        Guid taskItemId,
        CancellationToken cancellationToken);

    Task<Attachment?> GetEntityAsync(
        Guid taskItemId,
        Guid attachmentId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
