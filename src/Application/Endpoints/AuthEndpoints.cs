using AuthService.Abstractions.Auth;
using AuthService.Abstractions.Common;
using AuthService.Abstractions.Security;
using AuthService.Application.Contracts.Auth;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Persistence.EfCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        group.MapPost("/login", Login)
            .WithName("Login")
            .Produces<LoginResponse>(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(401);

        return endpoints;
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] IdentityDbContext context,
        [FromServices] IPasswordHasher passwordHasher,
        [FromServices] ITokenService tokenService,
        [FromServices] IDateTimeProvider dateTimeProvider,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var contact = await context.Contacts
            .FirstOrDefaultAsync(
                c => c.Value == request.Email.ToLowerInvariant() && 
                     c.Kind == ContactKind.Email,
                cancellationToken);

        if (contact == null)
        {
            return Results.Problem(
                detail: "Invalid email or password",
                statusCode: 401,
                title: "Authentication Failed");
        }

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == contact.UserId, cancellationToken);

        if (user == null || !user.IsActive())
        {
            return Results.Problem(
                detail: "Invalid email or password",
                statusCode: 401,
                title: "Authentication Failed");
        }

        var credential = await context.Credentials
            .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);

        if (credential == null)
        {
            return Results.Problem(
                detail: "Invalid email or password",
                statusCode: 401,
                title: "Authentication Failed");
        }

        if (!passwordHasher.VerifyPassword(request.Password, credential.PasswordHash))
        {
            return Results.Problem(
                detail: "Invalid email or password",
                statusCode: 401,
                title: "Authentication Failed");
        }

        var permissions = await GetUserPermissionsAsync(context, user.Id, cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(
            user.Id,
            user.Type.ToString(),
            permissions,
            new Dictionary<string, string>
            {
                ["email"] = contact.Value
            });

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        
        var refreshToken = await tokenService.GenerateRefreshTokenAsync(
            user.Id,
            ipAddress,
            userAgent,
            cancellationToken);

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900, // 15 minutes
            TokenType = "Bearer",
            User = new UserInfo
            {
                UserId = user.Id,
                UserType = user.Type.ToString(),
                Email = contact.Value,
                Permissions = permissions,
                MustChangePassword = credential.MustChangePassword
            }
        };

        return Results.Ok(response);
    }

    private static async Task<List<string>> GetUserPermissionsAsync(
        IdentityDbContext context,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var permissions = await context.UserSchoolMemberships
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
            .Join(
                context.RolePermissions,
                m => m.RoleId,
                rp => rp.RoleId,
                (m, rp) => rp.PermissionId)
            .Join(
                context.Permissions,
                permId => permId,
                p => p.Id,
                (permId, p) => p.PermCode)
            .Distinct()
            .ToListAsync(cancellationToken);

        return permissions;
    }
}

