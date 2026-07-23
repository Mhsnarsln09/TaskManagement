using TaskManagement.Application.Errors;

namespace TaskManagement.Infrastructure.Persistence;

// Turns the PostgreSQL xmin concurrency token (a uint) into an opaque string the client
// stores and echoes back, and back again for the concurrency check (B10-06). The client
// must treat the value as opaque; it is base64url so nothing invites arithmetic on it.
internal static class RowVersion
{
    public static string Encode(uint xmin)
    {
        return Convert.ToBase64String(BitConverter.GetBytes(xmin))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static uint Decode(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ConflictException("A version token is required. Refresh the task and retry.");
        }

        string padded = version.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');

        try
        {
            byte[] bytes = Convert.FromBase64String(padded);
            if (bytes.Length != sizeof(uint))
            {
                throw new ConflictException("The version token is not valid. Refresh the task and retry.");
            }

            return BitConverter.ToUInt32(bytes);
        }
        catch (FormatException)
        {
            throw new ConflictException("The version token is not valid. Refresh the task and retry.");
        }
    }
}
