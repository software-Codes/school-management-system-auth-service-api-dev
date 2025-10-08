using AuthService.Domain.Common;

namespace AuthService.Domain.Entities;

/// <summary>
/// Maps roles to permissions (many-to-many relationship)
/// </summary>
public class RolePermission : BaseEntity
{
    private RolePermission() { }

    public static RolePermission Create(
        Guid roleId,
        Guid permissionId,
        DateTime utcNow)
    {
        return new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    // Navigation properties
    public Role Role { get; private set; } = null!;
    public Permission Permission { get; private set; } = null!;
}

