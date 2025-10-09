using AuthService.Domain.Common;

namespace AuthService.Domain.Entities;

/// <summary>
/// Represents a role in the system (Principal, Teacher, Parent, etc.)
/// Roles are assigned to users per school via UserSchoolMemberships
/// </summary>
public class Role : BaseEntity
{
    private Role() { }

    public static Role Create(
        string roleCode,
        string description,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
            throw new ArgumentException("Role code cannot be empty", nameof(roleCode));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        return new Role
        {
            Id = Guid.NewGuid(),
            RoleCode = roleCode.Trim(),
            Description = description.Trim(),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public string RoleCode { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    public void UpdateDescription(string description, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        Description = description.Trim();
        MarkAsUpdated(utcNow);
    }
}

