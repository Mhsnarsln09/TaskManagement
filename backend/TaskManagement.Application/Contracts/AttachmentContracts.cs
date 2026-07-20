namespace TaskManagement.Application.Contracts;

// Transport-agnostic upload input. The Api maps IFormFile onto this so the
// Application layer never sees ASP.NET Core types.
public sealed record FileUpload(
    string FileName,
    string? ContentType,
    long SizeInBytes,
    Stream Content);

public sealed record AttachmentListItem(
    Guid Id,
    Guid TaskItemId,
    Guid UploadedByUserId,
    string FileName,
    string ContentType,
    long SizeInBytes,
    DateTimeOffset CreatedAtUtc);

public sealed record AttachmentResponse(
    Guid Id,
    Guid TaskItemId,
    string FileName,
    string ContentType,
    long SizeInBytes,
    UserSummaryResponse UploadedBy,
    DateTimeOffset CreatedAtUtc);

// The caller owns the stream and must dispose it after writing the response body.
public sealed record AttachmentDownload(
    string FileName,
    string ContentType,
    Stream Content);
