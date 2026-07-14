using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Comments;
using TaskManagement.Domain.Tasks;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Id)
            .ValueGeneratedNever();

        builder.Property(comment => comment.TaskItemId)
            .IsRequired();

        builder.Property(comment => comment.AuthorUserId)
            .IsRequired();

        builder.Property(comment => comment.Content)
            .HasMaxLength(2_000)
            .IsRequired();

        builder.Property(comment => comment.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(comment => comment.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(comment => comment.TaskItemId);
        builder.HasIndex(comment => comment.AuthorUserId);

        builder.HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(comment => comment.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(comment => comment.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
