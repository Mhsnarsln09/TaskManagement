using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Attachments;
using TaskManagement.Domain.Comments;
using TaskManagement.Domain.Projects;
using TaskManagement.Domain.Tasks;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // "xmin" is a PostgreSQL system column mapped as the optimistic concurrency
        // token. Integration tests run on real PostgreSQL (Testcontainers), so no
        // other provider needs to be accommodated here.
        builder.Entity<Project>().Property<uint>("xmin").IsRowVersion();
        builder.Entity<TaskItem>().Property<uint>("xmin").IsRowVersion();
    }
}
