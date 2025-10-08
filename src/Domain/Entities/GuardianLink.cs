using AuthService.Domain.Common;

namespace AuthService.Domain.Entities;

/// <summary>
/// Links a parent/guardian to a student
/// Requires verification for privacy/security
/// </summary>
public class GuardianLink : BaseEntity
{
    private GuardianLink() { }

    public static GuardianLink Create(
        Guid parentUserId,
        Guid studentId,
        Guid schoolId,
        string relationship,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(relationship))
            throw new ArgumentException("Relationship cannot be empty", nameof(relationship));

        return new GuardianLink
        {
            Id = Guid.NewGuid(),
            ParentUserId = parentUserId,
            StudentId = studentId,
            SchoolId = schoolId,
            Relationship = relationship,
            Status = "PendingVerification",
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public Guid ParentUserId { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Relationship { get; private set; } = null!;  // Mother, Father, Guardian
    public string Status { get; private set; } = null!;  // PendingVerification, Active, Revoked
    public DateTime? VerifiedAt { get; private set; }

    public void Verify(DateTime utcNow)
    {
        Status = "Active";
        VerifiedAt = utcNow;
        MarkAsUpdated(utcNow);
    }

    public void Revoke(DateTime utcNow)
    {
        Status = "Revoked";
        MarkAsUpdated(utcNow);
    }
}
