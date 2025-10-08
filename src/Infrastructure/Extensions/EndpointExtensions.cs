using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AuthService.Infrastructure.Extensions;

/// <summary>
/// Extension methods for mapping API endpoints
/// </summary>
public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapDiagnosticEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/version", () => new
        {
            version = "1.0.0",
            service = "AuthService",
            timestamp = DateTime.UtcNow
        })
        .WithName("GetVersion")
        .WithTags("Diagnostics")
        .WithOpenApi();

        return endpoints;
    }

    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // No checks, just return healthy if app is running
            AllowCachingResponses = false
        })
        .WithName("LivenessCheck")
        .WithTags("Health");

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            AllowCachingResponses = false,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds
                    })
                });
                await context.Response.WriteAsync(result);
            }
        })
        .WithName("ReadinessCheck")
        .WithTags("Health");

        return endpoints;
    }
}

