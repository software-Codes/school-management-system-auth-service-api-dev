namespace AuthService.Infrastructure.Persistence.Seeding.Models;

/// <summary>
/// Defines the permissions available in the system
/// </summary>
public sealed class PermissionSeedData
{
    public required string PermCode { get; init; }
    public required string Description { get; init; }
}

