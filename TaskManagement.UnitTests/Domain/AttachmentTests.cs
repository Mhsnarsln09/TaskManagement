using TaskManagement.Domain.Attachments;

namespace TaskManagement.UnitTests.Domain;

public sealed class AttachmentTests
{
    [Fact]
    public void Constructor_RequiresTaskId()
    {
        Assert.Throws<ArgumentException>(() => CreateAttachment(taskItemId: Guid.Empty));
    }

    [Fact]
    public void Constructor_RequiresUploader()
    {
        Assert.Throws<ArgumentException>(() => CreateAttachment(uploadedByUserId: Guid.Empty));
    }

    [Fact]
    public void Constructor_RequiresStoredFileName()
    {
        Assert.Throws<ArgumentException>(() => CreateAttachment(storedFileName: "  "));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveSize(long sizeInBytes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateAttachment(sizeInBytes: sizeInBytes));
    }

    [Fact]
    public void Constructor_KeepsDisplayNameAndStoredNameSeparate()
    {
        Attachment attachment = CreateAttachment();

        Assert.Equal("report.pdf", attachment.FileName);
        Assert.Equal("0f9a.pdf", attachment.StoredFileName);
    }

    private static Attachment CreateAttachment(
        Guid? taskItemId = null,
        Guid? uploadedByUserId = null,
        string fileName = "report.pdf",
        string storedFileName = "0f9a.pdf",
        string contentType = "application/pdf",
        long sizeInBytes = 128)
    {
        return new Attachment(
            Guid.NewGuid(),
            taskItemId ?? Guid.NewGuid(),
            uploadedByUserId ?? Guid.NewGuid(),
            fileName,
            storedFileName,
            contentType,
            sizeInBytes);
    }
}
