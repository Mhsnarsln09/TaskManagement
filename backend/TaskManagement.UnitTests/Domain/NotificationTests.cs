using TaskManagement.Domain.Notifications;

namespace TaskManagement.UnitTests.Domain;

public sealed class NotificationTests
{
    [Fact]
    public void MarkAsRead_IsIdempotent()
    {
        var notification = new Notification(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), NotificationType.TaskAssigned, "Assigned", "key");
        var firstRead = new DateTimeOffset(2026, 7, 20, 10, 0, 0, TimeSpan.Zero);

        notification.MarkAsRead(firstRead);
        notification.MarkAsRead(firstRead.AddHours(1));

        Assert.Equal(firstRead, notification.ReadAtUtc);
    }
}
