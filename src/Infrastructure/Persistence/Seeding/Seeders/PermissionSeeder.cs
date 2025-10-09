using AuthService.Abstractions.Common;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.Infrastructure.Persistence.Seeding.Data;
using AuthService.Infrastructure.Persistence.Seeding.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Persistence.Seeding.Seeders;

/// <summary>
/// Seeds permissions into the database
/// Single Responsibility: Only handles permission seeding
/// </summary>
public sealed class PermissionSeeder : IDatabaseSeeder
{
    private readonly IdentityDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<PermissionSeeder> _logger;

    public PermissionSeeder(
        IdentityDbContext context,
        IDateTimeProvider dateTimeProvider,
        ILogger<PermissionSeeder> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting permissions seeding...");

        var permissionsData = PermissionsData.GetAll();
        var now = _dateTimeProvider.UtcNow;

        foreach (var permData in permissionsData)
        {
            var exists = await _context.Permissions
                .AnyAsync(p => p.PermCode == permData.PermCode, cancellationToken);

            if (!exists)
            {
                var permission = Permission.Create(
                    permData.PermCode,
                    permData.Description,
                    now);

                await _context.Permissions.AddAsync(permission, cancellationToken);
                _logger.LogInformation("Created permission: {PermCode}", permData.PermCode);
            }
            else
            {
                _logger.LogDebug("Permission already exists: {PermCode}", permData.PermCode);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Permissions seeding completed. Total permissions: {Count}", permissionsData.Count);
    }
}

