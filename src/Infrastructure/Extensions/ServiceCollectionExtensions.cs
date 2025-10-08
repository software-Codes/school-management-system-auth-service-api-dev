using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.Abstractions.Common;
using AuthService.Infrastructure.Time;

namespace AuthService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        return services;
    }

    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("IdentityDb connection string is not configured.");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                if (configuration.GetValue<bool>("Db:EnableRetryOnFailure", true))
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: configuration.GetValue<int>("Db:MaxRetryCount", 5),
                        maxRetryDelay: TimeSpan.FromSeconds(configuration.GetValue<int>("Db:MaxRetryDelaySeconds", 10)),
                        errorNumbersToAdd: null);
                }

                sqlOptions.CommandTimeout(30);
            });

            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging", false))
            {
                options.EnableSensitiveDataLogging();
            }

            options.EnableDetailedErrors();
        });

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }

    public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("IdentityDb connection string is not configured.");

        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: connectionString,
                name: "sql-server",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sql", "ready" });

        return services;
    }

    public static IConfigurationBuilder AddAzureKeyVaultIfEnabled(this IConfigurationBuilder configuration, IConfiguration config)
    {
        var useKeyVault = config.GetValue<bool>("Azure:KeyVault:Enabled", false);
        
        if (useKeyVault)
        {
            var keyVaultUri = config["Azure:KeyVault:Uri"] 
                ?? throw new InvalidOperationException("Azure Key Vault URI is not configured.");
            
            var secretClient = new SecretClient(
                new Uri(keyVaultUri), 
                new DefaultAzureCredential());
            
            configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
        }

        return configuration;
    }
}
