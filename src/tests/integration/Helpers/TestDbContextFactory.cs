using AuthService.Infrastructure.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.IntegrationTests.Helpers;

/// <summary>
/// Factory for creating test database contexts with in-memory database
/// </summary>
public static class TestDbContextFactory
{
    public static IdentityDbContext CreateInMemoryContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new TestIdentityDbContext(options);
    }

    public static IdentityDbContext CreateInMemoryContextWithSharedName(string databaseName)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        return new TestIdentityDbContext(options);
    }
}

