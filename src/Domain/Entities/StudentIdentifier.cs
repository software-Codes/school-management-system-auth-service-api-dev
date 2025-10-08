using AuthService.Domain.Common;

namespace AuthService.Domain.Entities;

public class StudentIdentifier : BaseEntity
{
    private StudentIdentifier() { }

    public static StudentIdentifier Create(
        Guid studentId,
        Guid schoolId,
        string kind,  // "AdmissionNumber" or "UPI"
        string value,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Identifier value cannot be empty", nameof(value));

        return new StudentIdentifier
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            SchoolId = schoolId,
            Kind = kind,
            Value = value.Trim().ToUpperInvariant(), // Normalize
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public Guid StudentId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Kind { get; private set; } = null!;  // "AdmissionNumber", "UPI"
    public string Value { get; private set; } = null!;  // "CT201", "12345678"
}

