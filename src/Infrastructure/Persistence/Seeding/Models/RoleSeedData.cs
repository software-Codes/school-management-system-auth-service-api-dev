namespace AuthService.Infrastructure.Persistence.Seeding.Models;

/// <summary>
/// Defines the roles available in the system
/// </summary>
public sealed class RoleSeedData
{
    public required string RoleCode { get; init; }
    public required string Description { get; init; }
    public List<string> PermissionCodes { get; init; } = new();
}

