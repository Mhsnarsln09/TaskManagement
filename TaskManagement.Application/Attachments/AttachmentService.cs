using Microsoft.Extensions.Options;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;
using TaskManagement.Application.Files;
using TaskManagement.Application.Tasks;
using TaskManagement.Domain.Attachments;

namespace TaskManagement.Application.Attachments;

public sealed class AttachmentService(
    IAttachmentRepository attachmentRepository,
    IFileStorage fileStorage,
    IIdentityService identityService,
    TaskAccessGuard taskAccess,
    ICurrentUser currentUser,
    IOptions<FileUploadOptions> uploadOptions)
{
    private readonly FileUploadOptions _options = uploadOptions.Value;

    public async Task<AttachmentResponse> UploadAsync(
        Guid projectId,
        Guid taskId,
        FileUpload upload,
        CancellationToken cancellationToken)
    {
        await taskAccess.EnsureTaskAccessibleAsync(projectId, taskId, cancellationToken);

        (string fileName, string extension, string contentType) = ValidateUpload(upload);

        // The physical name is generated, never derived from the client name. This is
        // what makes path traversal structurally impossible instead of merely filtered.
        string storedFileName = $"{Guid.NewGuid():N}{extension}";

        var attachment = new Attachment(
            Guid.NewGuid(),
            taskId,
            currentUser.UserId,
            fileName,
            storedFileName,
            contentType,
            upload.SizeInBytes);

        await fileStorage.SaveAsync(storedFileName, upload.Content, cancellationToken);

        try
        {
            await attachmentRepository.AddAsync(attachment, cancellationToken);
            await attachmentRepository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Bytes are written before the metadata row, so a failed insert would
            // otherwise leave a file nothing points at. Metadata without bytes is the
            // worse failure (a download would 500), so the compensation runs this way
            // round. Delete failures must not mask the original error.
            await TryDeleteAsync(storedFileName, cancellationToken);
            throw;
        }

        IReadOnlyDictionary<Guid, UserSummaryResponse> uploaders =
            await identityService.GetUserSummariesAsync([attachment.UploadedByUserId], cancellationToken);

        return Map(
            new AttachmentListItem(
                attachment.Id,
                attachment.TaskItemId,
                attachment.UploadedByUserId,
                attachment.FileName,
                attachment.ContentType,
                attachment.SizeInBytes,
                attachment.CreatedAtUtc),
            uploaders);
    }

    public async Task<IReadOnlyCollection<AttachmentResponse>> ListAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        await taskAccess.EnsureTaskAccessibleAsync(projectId, taskId, cancellationToken);

        IReadOnlyCollection<AttachmentListItem> attachments =
            await attachmentRepository.ListAsync(taskId, cancellationToken);

        Guid[] uploaderIds = attachments
            .Select(attachment => attachment.UploadedByUserId)
            .Distinct()
            .ToArray();

        IReadOnlyDictionary<Guid, UserSummaryResponse> uploaders =
            await identityService.GetUserSummariesAsync(uploaderIds, cancellationToken);

        return attachments.Select(attachment => Map(attachment, uploaders)).ToList();
    }

    public async Task<AttachmentDownload> DownloadAsync(
        Guid projectId,
        Guid taskId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        // Downloads run the same membership check as every other read: a direct link
        // to the attachment id is not an authorization bypass.
        await taskAccess.EnsureTaskAccessibleAsync(projectId, taskId, cancellationToken);

        Attachment attachment = await attachmentRepository.GetEntityAsync(taskId, attachmentId, cancellationToken)
            ?? throw new NotFoundException("Attachment was not found.");

        Stream content = await fileStorage.OpenReadAsync(attachment.StoredFileName, cancellationToken)
            ?? throw new NotFoundException("Attachment content is no longer available.");

        return new AttachmentDownload(attachment.FileName, attachment.ContentType, content);
    }

    private (string FileName, string Extension, string ContentType) ValidateUpload(FileUpload upload)
    {
        var errors = new Dictionary<string, string[]>();

        string? fileName = UploadFileName.Sanitize(upload.FileName);
        string? extension = fileName is null ? null : UploadFileName.GetExtension(fileName);

        if (fileName is null)
        {
            errors["file"] = ["File name is missing or contains only unsupported characters."];
        }
        else if (extension is null)
        {
            errors["file"] = ["File name must have an extension."];
        }
        else if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            errors["file"] =
                [$"File extension is not allowed. Allowed extensions: {string.Join(", ", _options.AllowedExtensions)}."];
        }

        if (upload.SizeInBytes <= 0)
        {
            errors["file.size"] = ["File is empty."];
        }
        else if (upload.SizeInBytes > _options.MaxSizeInBytes)
        {
            errors["file.size"] = [$"File exceeds the maximum size of {_options.MaxSizeInBytes} bytes."];
        }

        // The MIME parameters (";charset=...") are not part of the allow-list match.
        string contentType = (upload.ContentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();
        if (!_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            errors["file.contentType"] =
                [$"Content type is not allowed. Allowed types: {string.Join(", ", _options.AllowedContentTypes)}."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationProblemException(errors);
        }

        return (fileName!, extension!, contentType);
    }

    private async Task TryDeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        try
        {
            await fileStorage.DeleteAsync(storedFileName, cancellationToken);
        }
        catch (IOException)
        {
            // The orphaned file is a housekeeping problem, not a request failure.
        }
    }

    private static AttachmentResponse Map(
        AttachmentListItem attachment,
        IReadOnlyDictionary<Guid, UserSummaryResponse> uploaders)
    {
        UserSummaryResponse uploadedBy =
            uploaders.TryGetValue(attachment.UploadedByUserId, out UserSummaryResponse? found)
                ? found
                : new UserSummaryResponse(attachment.UploadedByUserId, "unknown", null);

        return new AttachmentResponse(
            attachment.Id,
            attachment.TaskItemId,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeInBytes,
            uploadedBy,
            attachment.CreatedAtUtc);
    }
}
