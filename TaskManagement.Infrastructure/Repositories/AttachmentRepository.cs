using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Attachments;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class AttachmentRepository(ApplicationDbContext dbContext) : IAttachmentRepository
{
    public async Task AddAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        await dbContext.Attachments.AddAsync(attachment, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AttachmentListItem>> ListAsync(
        Guid taskItemId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Attachments
            .AsNoTracking()
            .Where(attachment => attachment.TaskItemId == taskItemId)
            .OrderBy(attachment => attachment.CreatedAtUtc)
            .ThenBy(attachment => attachment.Id)
            // StoredFileName is deliberately absent from the response projection: the
            // physical name is an internal detail clients never need.
            .Select(attachment => new AttachmentListItem(
                attachment.Id,
                attachment.TaskItemId,
                attachment.UploadedByUserId,
                attachment.FileName,
                attachment.ContentType,
                attachment.SizeInBytes,
                attachment.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<Attachment?> GetEntityAsync(
        Guid taskItemId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Attachments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                attachment => attachment.TaskItemId == taskItemId && attachment.Id == attachmentId,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
