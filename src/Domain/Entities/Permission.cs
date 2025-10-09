using AuthService.Domain.Common;

namespace AuthService.Domain.Entities;

/// <summary>
/// Represents a fine-grained permission in the system
/// Permissions are assigned to roles via RolePermissions
/// </summary>
public class Permission : BaseEntity
{
    private Permission() { }

    public static Permission Create(
        string permCode,
        string description,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(permCode))
            throw new ArgumentException("Permission code cannot be empty", nameof(permCode));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        return new Permission
        {
            Id = Guid.NewGuid(),
            PermCode = permCode.Trim(),
            Description = description.Trim(),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public string PermCode { get; private set; } = null!;
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

