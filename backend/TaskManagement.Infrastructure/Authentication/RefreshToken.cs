namespace TaskManagement.Infrastructure.Authentication;

public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid FamilyId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    public RefreshToken(Guid id, Guid userId, Guid familyId, string tokenHash, DateTimeOffset expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void RotateTo(Guid replacementId, DateTimeOffset now)
    {
        RevokedAtUtc = now;
        ReplacedByTokenId = replacementId;
    }

    public void Revoke(DateTimeOffset now) => RevokedAtUtc ??= now;

    private RefreshToken() { }
}
