namespace AuthService.Infrastructure.Auth;

public static class AuthorizationPolicies
{
    public const string SystemAdminOnly = "SystemAdminOnly";
    public const string RequireAuthentication = "RequireAuthentication";
}

public static class PermissionClaims
{
    public const string Permission = "permission";
    public const string UserType = "user_type";
}

