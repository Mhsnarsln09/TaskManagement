using System.Text;
using TaskManagement.Application.Abstractions;

namespace TaskManagement.Infrastructure.Files;

public sealed class BasicFileScanner : IFileScanner
{
    private static readonly byte[] EicarMarker = Encoding.ASCII.GetBytes("EICAR-STANDARD-ANTIVIRUS-TEST-FILE");

    public async Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray().AsSpan().IndexOf(EicarMarker) < 0;
    }
}
