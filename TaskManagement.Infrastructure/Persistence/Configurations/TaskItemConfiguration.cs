using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Projects;
using TaskManagement.Domain.Tasks;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("task_items");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .ValueGeneratedNever();

        builder.Property(task => task.ProjectId)
            .IsRequired();

        builder.Property(task => task.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(task => task.Description)
            .HasMaxLength(4_000);

        builder.Property(task => task.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(task => task.Priority)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(task => task.DueDate)
            .HasColumnType("date");

        builder.Property(task => task.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(task => task.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(task => task.DeletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(task => task.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(task => task.ProjectId);
        builder.HasIndex(task => task.AssigneeUserId);
        builder.HasIndex(task => task.Status);
        builder.HasIndex(task => task.DueDate);
        builder.HasIndex(task => new { task.ProjectId, task.Status, task.DueDate });

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(task => task.AssigneeUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
