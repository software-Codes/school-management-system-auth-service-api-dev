using AuthService.Domain.Common;

namespace AuthService.Domain.Entities;

/// <summary>
/// Student profile linked to a User account
/// </summary>
public class Student : BaseEntity
{
    private Student() { }

    public static Student Create(
        Guid userId,
        string? officialNumber,
        DateTime dateOfBirth,
        DateTime utcNow)
    {
        return new Student
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OfficialNumber = officialNumber,
            DateOfBirth = dateOfBirth,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public Guid UserId { get; private set; }
    public string? OfficialNumber { get; private set; }  // National student ID
    public DateTime DateOfBirth { get; private set; }
}

