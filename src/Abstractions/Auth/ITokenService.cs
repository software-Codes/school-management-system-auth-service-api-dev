namespace AuthService.Abstractions.Auth;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string userType, List<string> permissions, Dictionary<string, string>? additionalClaims = null);
    
    Task<string> GenerateRefreshTokenAsync(Guid userId, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
    
    Task<bool> ValidateRefreshTokenAsync(string refreshToken, Guid userId, CancellationToken cancellationToken = default);
    
    Task RevokeRefreshTokenAsync(string refreshToken, string reason, CancellationToken cancellationToken = default);
}

