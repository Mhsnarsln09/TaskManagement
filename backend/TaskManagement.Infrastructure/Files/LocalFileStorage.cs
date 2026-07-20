using Microsoft.Extensions.Options;
using TaskManagement.Application.Abstractions;

namespace TaskManagement.Infrastructure.Files;

// First IFileStorage implementation: a flat local directory. Object storage can
// replace it without touching the Application layer.
//
// The application already generates the stored name, so traversal should be
// impossible by construction. This class still validates it, because a storage
// backend must be safe on its own terms rather than trusting its caller.
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IOptions<LocalFileStorageOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken)
    {
        string path = ResolvePath(storedFileName);

        // CreateNew rather than Create: a name collision must fail loudly instead of
        // silently overwriting another task's attachment.
        await using var target = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81_920,
            useAsync: true);

        await content.CopyToAsync(target, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        string path = ResolvePath(storedFileName);

        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81_920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
    {
        File.Delete(ResolvePath(storedFileName));
        return Task.CompletedTask;
    }

    private string ResolvePath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName)
            || storedFileName.Contains('/')
            || storedFileName.Contains('\\')
            || storedFileName.Contains("..", StringComparison.Ordinal)
            || Path.GetFileName(storedFileName) != storedFileName)
        {
            throw new ArgumentException("Stored file name must be a single path segment.", nameof(storedFileName));
        }

        string path = Path.GetFullPath(Path.Combine(_rootPath, storedFileName));

        // Final backstop: whatever the name was, the resolved path must stay inside
        // the storage root.
        if (!path.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("Stored file name resolves outside the storage root.", nameof(storedFileName));
        }

        return path;
    }
}
