using TaskManagement.Application.Files;

namespace TaskManagement.UnitTests.Application;

// The sanitizer is the first line of defence for uploads, so it is tested against the
// shapes a hostile client actually sends rather than only the happy path.
public sealed class UploadFileNameTests
{
    [Theory]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData("  report.pdf  ", "report.pdf")]
    [InlineData("../../../etc/passwd", "passwd")]
    // Backslashes must be stripped on every platform, not just Windows.
    [InlineData("..\\..\\windows\\system32\\cmd.exe", "cmd.exe")]
    [InlineData("C:\\temp\\notes.txt", "notes.txt")]
    [InlineData("/absolute/path/notes.txt", "notes.txt")]
    // A NUL byte is a classic native-API truncation trick.
    [InlineData("image.png\0.exe", "image.png.exe")]
    public void Sanitize_ReducesHostileNamesToASingleSegment(string input, string expected)
    {
        Assert.Equal(expected, UploadFileName.Sanitize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("/")]
    [InlineData("///")]
    public void Sanitize_RejectsNamesWithNothingUsableLeft(string? input)
    {
        Assert.Null(UploadFileName.Sanitize(input));
    }

    [Fact]
    public void Sanitize_RejectsOverlyLongNames()
    {
        string input = new string('a', 256) + ".txt";

        Assert.Null(UploadFileName.Sanitize(input));
    }

    [Theory]
    [InlineData("report.PDF", ".pdf")]
    [InlineData("archive.tar.gz", ".gz")]
    public void GetExtension_ReturnsTheLastExtensionInLowerCase(string input, string expected)
    {
        Assert.Equal(expected, UploadFileName.GetExtension(input));
    }

    [Theory]
    // No dot at all, and a leading dot only (already trimmed by Sanitize) both mean
    // "no extension" rather than an empty one, so the allow-list check fails closed.
    [InlineData("report")]
    [InlineData(".gitignore")]
    public void GetExtension_ReturnsNullWhenThereIsNoUsableExtension(string input)
    {
        Assert.Null(UploadFileName.GetExtension(input));
    }
}
