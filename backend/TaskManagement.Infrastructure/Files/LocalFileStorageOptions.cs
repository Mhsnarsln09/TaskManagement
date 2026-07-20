namespace TaskManagement.Infrastructure.Files;

public sealed class LocalFileStorageOptions
{
    public const string SectionName = "FileStorage:Local";

    // Relative paths resolve against the content root, which keeps development
    // simple; deployments should point this at a volume outside the application
    // directory so uploaded files are never served as static content.
    public string RootPath { get; init; } = "App_Data/attachments";
}
