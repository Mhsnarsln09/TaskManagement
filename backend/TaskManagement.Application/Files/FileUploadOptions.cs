namespace TaskManagement.Application.Files;

// Upload policy is configuration, not code: limits and allow-lists change per
// environment and must be reviewable without a rebuild. The physical location of the
// files is deliberately not here — that belongs to the storage implementation.
public sealed class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    public long MaxSizeInBytes { get; init; } = 5 * 1024 * 1024;

    // Allow-list, never a deny-list: an unknown extension is rejected by default.
    public string[] AllowedExtensions { get; init; } =
    [
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".pdf",
        ".txt",
        ".csv"
    ];

    // The declared content type is client-supplied and therefore not proof of the
    // file's real format; it is allow-listed as a cheap first filter. The defence
    // that matters is on the way out: downloads are always sent as an attachment
    // with nosniff, so a mislabelled file cannot be rendered by the browser.
    public string[] AllowedContentTypes { get; init; } =
    [
        "image/png",
        "image/jpeg",
        "image/gif",
        "application/pdf",
        "text/plain",
        "text/csv"
    ];
}
