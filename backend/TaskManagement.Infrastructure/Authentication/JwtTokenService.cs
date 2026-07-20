using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;
using TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore.Storage;

namespace TaskManagement.Infrastructure.Authentication;

public sealed class JwtTokenService(
    IOptions<JwtOptions> jwtOptions,
    ApplicationDbContext dbContext,
    IIdentityService identityService,
    TimeProvider timeProvider) : IAccessTokenGenerator
{
    public async Task<AuthResponse> CreateSessionAsync(UserResponse user, CancellationToken cancellationToken)
    {
        return await CreateSessionAsync(user, Guid.NewGuid(), cancellationToken);
    }

    public async Task<AuthResponse> RotateAsync(string refreshToken, CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        string hash = Hash(refreshToken);

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // The row lock serializes rotations of the same token. A second request waits
        // for the first transaction, then observes the committed revoked state and
        // triggers reuse detection instead of issuing another active token.
        RefreshToken? current = await dbContext.RefreshTokens
            .FromSqlInterpolated($"""
                SELECT *
                FROM refresh_tokens
                WHERE "TokenHash" = {hash}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (current is null || current.ExpiresAtUtc <= now)
        {
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        }

        if (current.RevokedAtUtc is not null)
        {
            List<RefreshToken> family = await dbContext.RefreshTokens
                .Where(token => token.FamilyId == current.FamilyId && token.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);
            family.ForEach(token => token.Revoke(now));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedException("Refresh token reuse was detected. The session has been revoked.");
        }

        UserResponse user = await identityService.GetUserAsync(current.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Refresh token user no longer exists.");

        Guid replacementId = Guid.NewGuid();
        current.RotateTo(replacementId, now);
        AuthResponse response = await CreateSessionAsync(
            user, current.FamilyId, cancellationToken, replacementId);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    private async Task<AuthResponse> CreateSessionAsync(
        UserResponse user,
        Guid familyId,
        CancellationToken cancellationToken,
        Guid? refreshTokenId = null)
    {
        JwtOptions options = jwtOptions.Value;
        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset expiresAtUtc = now.AddMinutes(options.ExpiryMinutes);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email)
        ];

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        string plainRefreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        DateTimeOffset refreshExpiresAtUtc = now.AddDays(options.RefreshTokenExpiryDays);
        var storedRefreshToken = new RefreshToken(
            refreshTokenId ?? Guid.NewGuid(),
            user.Id,
            familyId,
            Hash(plainRefreshToken),
            refreshExpiresAtUtc);
        await dbContext.RefreshTokens.AddAsync(storedRefreshToken, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, expiresAtUtc, plainRefreshToken, refreshExpiresAtUtc, user);
    }

    private static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
