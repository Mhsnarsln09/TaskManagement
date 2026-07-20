using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Projects;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Id)
            .ValueGeneratedNever();

        builder.Property(project => project.Name)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasMaxLength(2_000);

        builder.Property(project => project.OwnerUserId)
            .IsRequired();

        builder.Property(project => project.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(project => project.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(project => project.DeletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(project => project.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(project => project.OwnerUserId);
        builder.HasIndex(project => project.IsDeleted);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(project => project.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(project => project.Members)
            .WithOne()
            .HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(project => project.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
