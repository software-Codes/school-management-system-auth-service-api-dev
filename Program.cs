using AuthService.Application.Endpoints;
using AuthService.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddAzureKeyVaultIfEnabled(builder.Configuration);

// Services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHealthChecksConfiguration(builder.Configuration);

var app = builder.Build();

// Middleware
app.UseHttpsRedirection();

// Endpoints
app.MapDiagnosticEndpoints();
app.MapHealthCheckEndpoints();
app.MapDatabaseEndpoints();

app.Run();

