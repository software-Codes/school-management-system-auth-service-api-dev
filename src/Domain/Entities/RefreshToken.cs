using AuthService.Domain.Common;

namespace AuthService.Domain.Entities;

/// <summary>
/// Refresh token for session management
/// Supports token rotation and revocation
/// </summary>
public class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime issuedAt,
        DateTime expiresAt,
        string? ipAddress,
        string? userAgent,
        DateTime utcNow)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime IssuedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string reason, DateTime utcNow, Guid? replacedByTokenId = null)
    {
        RevokedAt = utcNow;
        RevokedReason = reason;
        ReplacedByTokenId = replacedByTokenId;
        MarkAsUpdated(utcNow);
    }
}

