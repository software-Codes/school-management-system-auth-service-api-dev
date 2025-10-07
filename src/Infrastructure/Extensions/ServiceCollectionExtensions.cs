using Microsoft.Extensions.Diagnostics.HealthChecks;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace AuthService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        
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
