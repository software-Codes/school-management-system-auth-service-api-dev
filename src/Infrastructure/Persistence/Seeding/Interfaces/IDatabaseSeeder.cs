namespace AuthService.Infrastructure.Persistence.Seeding.Interfaces;

/// <summary>
/// Interface for database seeding operations
/// Following Interface Segregation Principle (SOLID)
/// </summary>
public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

