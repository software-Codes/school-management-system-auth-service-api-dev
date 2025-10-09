using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.Infrastructure.Persistence.Seeding.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Persistence.Seeding;

public sealed class DatabaseSeederService
{
    private readonly IdentityDbContext _context;
    private readonly IEnumerable<IDatabaseSeeder> _seeders;
    private readonly ILogger<DatabaseSeederService> _logger;

    public DatabaseSeederService(
        IdentityDbContext context,
        IEnumerable<IDatabaseSeeder> seeders,
        ILogger<DatabaseSeederService> logger)
    {
        _context = context;
        _seeders = seeders;
        _logger = logger;
    }

    public async Task SeedAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            
            if (!canConnect)
            {
                _logger.LogWarning("Database not available. Seeding skipped.");
                return;
            }

            // Check if already seeded (SystemAdmin exists)
            var adminExists = await _context.Users
                .AnyAsync(u => u.Type == Domain.Enums.UserType.SystemAdmin, cancellationToken);

            if (adminExists)
            {
                _logger.LogDebug("Database already seeded.");
                return;
            }

            _logger.LogInformation("🌱 First-time setup: Seeding database...");

            foreach (var seeder in _seeders)
            {
                await seeder.SeedAsync(cancellationToken);
            }

            _logger.LogInformation("✅ Database seeded! Email: admin@platform.local | Password: ChangeMe123!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database seeding failed");
            throw;
        }
    }
}
