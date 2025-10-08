using AuthService.Domain.Common;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;

/// <summary>
/// Represents an email or phone contact for a user
/// Supports multiple contacts per user (personal email, work email, mobile, etc.)
/// </summary>
public class Contact : BaseEntity
{
    private Contact() { }

    public static Contact Create(
        Guid userId,
        ContactKind kind,
        string value,
        bool isPrimary,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Contact value cannot be empty", nameof(value));

        return new Contact
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = kind,
            Value = value.Trim().ToLowerInvariant(), // Normalize
            IsPrimary = isPrimary,
            IsVerified = false,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public Guid UserId { get; private set; }
    public ContactKind Kind { get; private set; }
    public string Value { get; private set; } = null!;
    public bool IsPrimary { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    public void MarkAsVerified(DateTime utcNow)
    {
        IsVerified = true;
        VerifiedAt = utcNow;
        MarkAsUpdated(utcNow);
    }

    public void SetAsPrimary(DateTime utcNow)
    {
        IsPrimary = true;
        MarkAsUpdated(utcNow);
    }

    public void RemoveAsPrimary(DateTime utcNow)
    {
        IsPrimary = false;
        MarkAsUpdated(utcNow);
    }
}
