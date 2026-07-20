using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Attachments;
using TaskManagement.Domain.Comments;
using TaskManagement.Domain.Projects;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Notifications;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Application.Abstractions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TaskManagement.Infrastructure.Authentication;

namespace TaskManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly IAuditContext? _auditContext;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IAuditContext? auditContext = null)
        : base(options)
    {
        _auditContext = auditContext;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddAuditEntries();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AddAuditEntries()
    {
        EntityEntry[] changes = ChangeTracker.Entries()
            .Where(entry => entry.Entity is not AuditLog
                && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();

        foreach (EntityEntry entry in changes)
        {
            string entityId = entry.Properties
                .FirstOrDefault(property => property.Metadata.Name == "Id")?.CurrentValue?.ToString()
                ?? "unknown";
            AuditLogs.Add(new AuditLog(
                entry.State.ToString(),
                entry.Metadata.ClrType.Name,
                entityId,
                _auditContext?.UserId,
                _auditContext?.CorrelationId));
        }
    }

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // "xmin" is a PostgreSQL system column mapped as the optimistic concurrency
        // token. Integration tests run on real PostgreSQL (Testcontainers), so no
        // other provider needs to be accommodated here.
        builder.Entity<Project>().Property<uint>("xmin").IsRowVersion();
        builder.Entity<TaskItem>().Property<uint>("xmin").IsRowVersion();
        builder.Entity<Project>().HasQueryFilter(project => !project.IsDeleted);
        builder.Entity<TaskItem>().HasQueryFilter(task => !task.IsDeleted);
    }
}
