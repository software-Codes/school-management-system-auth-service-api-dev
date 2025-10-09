namespace AuthService.Application.Contracts.Auth;

public sealed record LoginResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required int ExpiresIn { get; init; }
    public required string TokenType { get; init; } = "Bearer";
    public required UserInfo User { get; init; }
}

public sealed record UserInfo
{
    public required Guid UserId { get; init; }
    public required string UserType { get; init; }
    public required string Email { get; init; }
    public required List<string> Permissions { get; init; }
    public required bool MustChangePassword { get; init; }
}

