using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Notifications;
using TaskManagement.Domain.Tasks;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.ProjectId).IsRequired();
        builder.Property(item => item.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(item => item.Message).HasMaxLength(500).IsRequired();
        builder.Property(item => item.IdempotencyKey).HasMaxLength(180).IsRequired();
        builder.Property(item => item.CreatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(item => item.UpdatedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(item => item.ReadAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(item => item.IdempotencyKey).IsUnique();
        builder.HasIndex(item => new { item.UserId, item.CreatedAtUtc });
        // Supports navigation/filtering by the notification's source project (B10-07).
        builder.HasIndex(item => item.ProjectId);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(item => item.TaskItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
