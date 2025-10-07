using AuthService.Domain.Common;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;

public class Credential : BaseEntity
{
    private Credential() { }

    public static Credential Create(
        Guid userId,
        byte[] passwordHash,
        MfaMode mfaMode,
        bool mustChangePassword,
        DateTime utcNow)
    {
        if (passwordHash == null || passwordHash.Length == 0)
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        return new Credential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PasswordHash = passwordHash,
            MfaMode = mfaMode,
            MustChangePassword = mustChangePassword,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public Guid UserId { get; private set; }
    public byte[] PasswordHash { get; private set; } = null!;
    public MfaMode MfaMode { get; private set; }
    public string? TotpSecret { get; private set; }
    public bool MustChangePassword { get; private set; }
    public DateTime? LastPasswordChangedAt { get; private set; }

    public void UpdatePassword(byte[] newPasswordHash, DateTime utcNow)
    {
        PasswordHash = newPasswordHash;
        LastPasswordChangedAt = utcNow;
        MustChangePassword = false; // Password changed, no longer required
        MarkAsUpdated(utcNow);
    }

    public void EnableTotp(string totpSecret, DateTime utcNow)
    {
        TotpSecret = totpSecret;
        MfaMode = MfaMode.PasswordAndTotp;
        MarkAsUpdated(utcNow);
    }

    public void DisableTotp(DateTime utcNow)
    {
        TotpSecret = null;
        MfaMode = MfaMode.PasswordOnly;
        MarkAsUpdated(utcNow);
    }

    public void RequirePasswordChange(DateTime utcNow)
    {
        MustChangePassword = true;
        MarkAsUpdated(utcNow);
    }
}
