using AuthService.Domain.Common;

namespace AuthService.Domain.Entities;

public class Username : BaseEntity
{
    private Username() { }

    public static Username Create(
        Guid userId,
        Guid schoolId,
        string username,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        return new Username
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SchoolId = schoolId,
            Value = username.Trim().ToLowerInvariant(), 
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public Guid UserId { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Value { get; private set; } = null!;  
}

