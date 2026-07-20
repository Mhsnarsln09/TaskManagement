namespace TaskManagement.Domain.Common;

public abstract class SoftDeletableEntity : Entity
{
    protected SoftDeletableEntity(Guid id) : base(id) { }

    protected SoftDeletableEntity() { }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public void SoftDelete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
        MarkUpdated();
    }
}
