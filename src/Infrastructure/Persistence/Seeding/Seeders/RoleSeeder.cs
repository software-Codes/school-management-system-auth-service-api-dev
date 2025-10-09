using AuthService.Abstractions.Common;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.Infrastructure.Persistence.Seeding.Data;
using AuthService.Infrastructure.Persistence.Seeding.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Persistence.Seeding.Seeders;

/// <summary>
/// Seeds roles and role-permission mappings into the database
/// Single Responsibility: Only handles role seeding
/// </summary>
public sealed class RoleSeeder : IDatabaseSeeder
{
    private readonly IdentityDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RoleSeeder> _logger;

    public RoleSeeder(
        IdentityDbContext context,
        IDateTimeProvider dateTimeProvider,
        ILogger<RoleSeeder> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting roles seeding...");

        var rolesData = RolesData.GetAll();
        var now = _dateTimeProvider.UtcNow;

        foreach (var roleData in rolesData)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.RoleCode == roleData.RoleCode, cancellationToken);

            if (role == null)
            {
                // Create new role
                role = Role.Create(roleData.RoleCode, roleData.Description, now);
                await _context.Roles.AddAsync(role, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken); // Save to get ID
                
                _logger.LogInformation("Created role: {RoleCode}", roleData.RoleCode);
            }
            else
            {
                _logger.LogDebug("Role already exists: {RoleCode}", roleData.RoleCode);
            }

            // Assign permissions to role
            await AssignPermissionsToRoleAsync(role.Id, roleData.PermissionCodes, now, cancellationToken);
        }

        _logger.LogInformation("Roles seeding completed. Total roles: {Count}", rolesData.Count);
    }

    private async Task AssignPermissionsToRoleAsync(
        Guid roleId,
        List<string> permissionCodes,
        DateTime now,
        CancellationToken cancellationToken)
    {
        foreach (var permCode in permissionCodes)
        {
            var permission = await _context.Permissions
                .FirstOrDefaultAsync(p => p.PermCode == permCode, cancellationToken);

            if (permission == null)
            {
                _logger.LogWarning("Permission not found: {PermCode}. Skipping.", permCode);
                continue;
            }

            var exists = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id, cancellationToken);

            if (!exists)
            {
                var rolePermission = RolePermission.Create(roleId, permission.Id, now);
                await _context.RolePermissions.AddAsync(rolePermission, cancellationToken);
                _logger.LogDebug("Assigned permission {PermCode} to role {RoleId}", permCode, roleId);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

