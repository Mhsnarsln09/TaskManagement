namespace TaskManagement.Application.Files;

// Client-supplied file names are hostile input. This helper reduces them to a plain
// display name and derives the extension used for the allow-list check; it never
// produces anything that is used as a path.
//
// Path.GetFileName is intentionally avoided: it is platform dependent, so on Linux it
// would keep "..\\..\\etc\\passwd" intact. Both separators are stripped explicitly.
internal static class UploadFileName
{
    private const int MaxLength = 255;

    public static string? Sanitize(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        string name = fileName.Trim();

        int separator = name.LastIndexOfAny(['/', '\\', ':']);
        if (separator >= 0)
        {
            name = name[(separator + 1)..];
        }

        // Control characters (including the NUL byte used to truncate names in
        // native file APIs) and reserved characters are dropped outright.
        name = new string(name.Where(IsAllowed).ToArray()).Trim().Trim('.');

        if (name.Length == 0 || name.Length > MaxLength)
        {
            return null;
        }

        return name;
    }

    public static string? GetExtension(string sanitizedFileName)
    {
        int dot = sanitizedFileName.LastIndexOf('.');
        if (dot <= 0 || dot == sanitizedFileName.Length - 1)
        {
            return null;
        }

        return sanitizedFileName[dot..].ToLowerInvariant();
    }

    private static bool IsAllowed(char character)
    {
        return !char.IsControl(character)
            && character is not ('/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|');
    }
}
