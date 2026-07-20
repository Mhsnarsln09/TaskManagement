using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Action).HasMaxLength(20).IsRequired();
        builder.Property(item => item.EntityType).HasMaxLength(120).IsRequired();
        builder.Property(item => item.EntityId).HasMaxLength(80).IsRequired();
        builder.Property(item => item.CorrelationId).HasMaxLength(100);
        builder.Property(item => item.Details).HasMaxLength(2_000);
        builder.Property(item => item.OccurredAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(item => item.OccurredAtUtc);
        builder.HasIndex(item => new { item.EntityType, item.EntityId });
    }
}
