namespace TaskManagement.Application.Abstractions;

// Port for the physical storage of attachment bytes. The first implementation is a
// local directory; swapping it for object storage must not touch the Application
// layer, so the port speaks only in opaque stored names and streams.
//
// Implementations must treat storedFileName as a flat, single-segment key and reject
// anything that could escape their own storage root.
public interface IFileStorage
{
    Task SaveAsync(string storedFileName, Stream content, CancellationToken cancellationToken);

    // Returns null when the key does not exist, so the caller can turn a missing
    // physical file into a 404 rather than a 500.
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken);

    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}
