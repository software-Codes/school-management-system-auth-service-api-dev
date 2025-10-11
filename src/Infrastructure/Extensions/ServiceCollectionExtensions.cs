using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.Abstractions.Common;
using AuthService.Infrastructure.Time;
using AuthService.Infrastructure.Persistence.Seeding;
using AuthService.Infrastructure.Persistence.Seeding.Interfaces;
using AuthService.Infrastructure.Persistence.Seeding.Seeders;
using AuthService.Abstractions.Security;
using AuthService.Abstractions.Auth;
using AuthService.Infrastructure.Security;
using AuthService.Infrastructure.Middleware;

namespace AuthService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<ITokenService, AuthService.Infrastructure.Auth.JwtTokenService>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Read from Key Vault (Jwt--SecretKey) or appsettings (Jwt:SecretKey)
        var secretKey = configuration["Jwt--SecretKey"] ?? configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey not configured in Key Vault or appsettings");
        var issuer = configuration["Jwt--Issuer"] ?? configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer not configured");
        var audience = configuration["Jwt--Audience"] ?? configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience not configured");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthService.Infrastructure.Auth.AuthorizationPolicies.SystemAdminOnly, policy =>
                policy.RequireClaim(AuthService.Infrastructure.Auth.PermissionClaims.UserType, "SystemAdmin"));

            options.AddPolicy(AuthService.Infrastructure.Auth.AuthorizationPolicies.RequireAuthentication, policy =>
                policy.RequireAuthenticatedUser());
        });

        return services;
    }

    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Read from Key Vault (ConnectionStrings--IdentityDb) or appsettings (ConnectionStrings:IdentityDb)
        var connectionString = configuration["ConnectionStrings--IdentityDb"] ?? configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("IdentityDb connection string not configured in Key Vault or appsettings");

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

    public static IServiceCollection AddDatabaseSeedingServices(this IServiceCollection services)
    {
        services.AddScoped<IDatabaseSeeder, PermissionSeeder>();
        services.AddScoped<IDatabaseSeeder, RoleSeeder>();
        services.AddScoped<IDatabaseSeeder, SystemAdminSeeder>();

        services.AddScoped<DatabaseSeederService>();

        return services;
    }

    public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Read from Key Vault (ConnectionStrings--IdentityDb) or appsettings (ConnectionStrings:IdentityDb)
        var connectionString = configuration["ConnectionStrings--IdentityDb"] ?? configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("IdentityDb connection string not configured in Key Vault or appsettings");

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

    public static IServiceCollection AddCommunicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuthService.Abstractions.Common.ICommunicationService, AuthService.Infrastructure.Communication.AzureCommunicationService>();
        return services;
    }

}
