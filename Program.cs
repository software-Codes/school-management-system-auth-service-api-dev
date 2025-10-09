using AuthService.Application.Endpoints;
using AuthService.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddAzureKeyVaultIfEnabled(builder.Configuration);

// Services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSecurityServices(builder.Configuration);  
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddDatabaseServices(builder.Configuration); 
builder.Services.AddDatabaseSeedingServices(); 
builder.Services.AddHealthChecksConfiguration(builder.Configuration);

var app = builder.Build();

// Seed database
await app.SeedDatabaseAsync();

// Middleware
app.UseExceptionHandler(); 
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapDiagnosticEndpoints();
app.MapHealthCheckEndpoints();
app.MapDatabaseEndpoints();
app.MapAuthEndpoints();

app.Run();

