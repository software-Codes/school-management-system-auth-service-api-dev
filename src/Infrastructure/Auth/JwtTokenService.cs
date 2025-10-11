using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Abstractions.Auth;
using AuthService.Abstractions.Common;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Auth;

public sealed class JwtTokenService : ITokenService
{
    private readonly IdentityDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(
        IdentityDbContext context,
        IDateTimeProvider dateTimeProvider,
        IConfiguration configuration,
        ILogger<JwtTokenService> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateAccessToken(
        Guid userId,
        string userType,
        List<string> permissions,
        Dictionary<string, string>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("user_type", userType),
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        // Read from Key Vault (Jwt--SecretKey) or appsettings (Jwt:SecretKey)
        var secretKey = _configuration["Jwt--SecretKey"] ?? _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT secret key not configured in Key Vault or appsettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = _dateTimeProvider.UtcNow;

        var issuer = _configuration["Jwt--Issuer"] ?? _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt--Audience"] ?? _configuration["Jwt:Audience"];
        var accessTokenMinutes = _configuration.GetValue<int>("Jwt:AccessTokenMinutes", 15);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(accessTokenMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogDebug("Generated access token for user {UserId} with {PermissionCount} permissions",
            userId, permissions.Count);

        return tokenString;
    }

    public async Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        string ipAddress,
        string userAgent,
        CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var tokenValue = GenerateSecureToken();
        var tokenHash = HashToken(tokenValue);

        var refreshToken = RefreshToken.Create(
            userId,
            tokenHash,
            now,
            now.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenDays", 30)),
            ipAddress,
            userAgent,
            now
        );

        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated refresh token for user {UserId}", userId);

        return tokenValue;
    }

    public async Task<bool> ValidateRefreshTokenAsync(
        string refreshToken,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var now = _dateTimeProvider.UtcNow;

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash &&
                     t.UserId == userId &&
                     t.ExpiresAt > now &&
                     t.RevokedAt == null,
                cancellationToken);

        return storedToken != null;
    }

    public async Task RevokeRefreshTokenAsync(
        string refreshToken,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var now = _dateTimeProvider.UtcNow;

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken != null)
        {
            storedToken.Revoke(reason, now);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Revoked refresh token for user {UserId}. Reason: {Reason}",
                storedToken.UserId, reason);
        }
    }

    private string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}

