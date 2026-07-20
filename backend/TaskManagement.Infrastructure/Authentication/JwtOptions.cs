namespace TaskManagement.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int ExpiryMinutes { get; init; } = 60;

    public int RefreshTokenExpiryDays { get; init; } = 14;

    public int ClockSkewSeconds { get; init; } = 30;
}
