namespace TaskManagement.Application.Abstractions;

public interface IFileScanner
{
    Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken);
}
