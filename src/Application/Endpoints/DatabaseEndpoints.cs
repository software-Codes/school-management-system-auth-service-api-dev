using Microsoft.Data.SqlClient;

namespace AuthService.Application.Endpoints;

public static class DatabaseEndpoints
{
    public static IEndpointRouteBuilder MapDatabaseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/db/test", TestDatabaseConnection)
            .WithName("TestDatabaseConnection")
            .WithTags("Diagnostics")
            .WithOpenApi();

        return endpoints;
    }

    private static async Task<IResult> TestDatabaseConnection(IConfiguration config)
    {
        var connectionString = config.GetConnectionString("IdentityDb");

        if (string.IsNullOrEmpty(connectionString))
        {
            return Results.Problem(
                detail: "Database connection string is not configured.",
                title: "Configuration Error",
                statusCode: 500);
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT @@VERSION as Version, DB_NAME() as DatabaseName, GETDATE() as ServerTime";

            await using var reader = await command.ExecuteReaderAsync();

            var results = new Dictionary<string, object>();
            if (await reader.ReadAsync())
            {
                results["sqlVersion"] = reader["Version"].ToString()!;
                results["database"] = reader["DatabaseName"].ToString()!;
                results["serverTime"] = reader["ServerTime"];
                results["status"] = "Connected";
                results["connectionType"] = "Azure SQL Database";
            }

            return Results.Ok(results);
        }
        catch (SqlException ex)
        {
            return Results.Problem(
                detail: $"SQL Error: {ex.Message}",
                title: "Database Connection Failed",
                statusCode: 503);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                title: "Database Connection Failed",
                statusCode: 503);
        }
    }
}
