using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Attachments;
using TaskManagement.Domain.Tasks;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments");

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id)
            .ValueGeneratedNever();

        builder.Property(attachment => attachment.TaskItemId)
            .IsRequired();

        builder.Property(attachment => attachment.UploadedByUserId)
            .IsRequired();

        builder.Property(attachment => attachment.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(attachment => attachment.StoredFileName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(attachment => attachment.ContentType)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(attachment => attachment.SizeInBytes)
            .IsRequired();

        builder.Property(attachment => attachment.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(attachment => attachment.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(attachment => attachment.TaskItemId);

        // One physical file backs exactly one metadata row, so a delete can never
        // orphan bytes another row still points at.
        builder.HasIndex(attachment => attachment.StoredFileName)
            .IsUnique();

        builder.HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(attachment => attachment.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(attachment => attachment.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
