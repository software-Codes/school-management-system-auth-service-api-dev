using AuthService.Infrastructure.Persistence.Seeding;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    public static async Task<IApplicationBuilder> SeedDatabaseAsync(
        this IApplicationBuilder app,
        CancellationToken cancellationToken = default)
    {
        using var scope = app.ApplicationServices.CreateScope();
        
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeederService>();
            await seeder.SeedAllAsync(cancellationToken);
        }
        catch
        {
            // Silently continue - app can still start
        }

        return app;
    }
}
