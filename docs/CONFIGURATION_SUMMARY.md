# Configuration Summary

## Overview

The Auth Service now supports **Azure Key Vault** for all sensitive configuration values. The application uses a **fallback mechanism**: it first tries to read from Key Vault, and if not found or Key Vault is disabled, it falls back to `appsettings.json`.

## Key Vault vs appsettings.json

| Source | Separator | Example Key |
|--------|-----------|-------------|
| **Key Vault** | `--` (double dash) | `Jwt--SecretKey` |
| **appsettings.json** | `:` (colon) | `Jwt:SecretKey` |

The code automatically tries both formats, with Key Vault taking precedence.

## All Configuration Values

### 🔐 Secrets (Should be in Key Vault in Production)

| appsettings.json | Key Vault Secret Name | Description | Required |
|------------------|----------------------|-------------|----------|
| `ConnectionStrings:IdentityDb` | `ConnectionStrings--IdentityDb` | SQL Server connection string | ✅ Yes |
| `Jwt:SecretKey` | `Jwt--SecretKey` | JWT signing key (256-bit minimum) | ✅ Yes |
| `Jwt:Issuer` | `Jwt--Issuer` | JWT token issuer | ✅ Yes |
| `Jwt:Audience` | `Jwt--Audience` | JWT token audience | ✅ Yes |
| `AzureCommunication:ConnectionString` | `AzureCommunication--ConnectionString` | Azure Communication Services connection | ✅ Yes |
| `AzureCommunication:FromPhoneNumber` | `AzureCommunication--FromPhoneNumber` | SMS sender phone number | ✅ Yes |
| `AzureCommunication:FromEmailAddress` | `AzureCommunication--FromEmailAddress` | Email sender address | ✅ Yes |
| `SeedData:SystemAdmin:Email` | `SeedData--SystemAdmin--Email` | System admin email for seeding | ❌ No (default: admin@platform.local) |
| `SeedData:SystemAdmin:TempPassword` | `SeedData--SystemAdmin--TempPassword` | System admin temp password | ❌ No (default: ChangeMe123!) |
| `Redis:Connection` | `Redis--Connection` | Redis connection string | ❌ Future use |
| `ServiceBus:Connection` | `ServiceBus--Connection` | Service Bus connection string | ❌ Future use |

### ⚙️ Non-Secret Configuration (Can stay in appsettings.json)

These values are not secrets and can remain in `appsettings.json`:

- `Jwt:AccessTokenMinutes` - Access token expiration (default: 15)
- `Jwt:RefreshTokenDays` - Refresh token expiration (default: 30)
- `Db:EnableRetryOnFailure` - Enable database retry logic (default: true)
- `Db:MaxRetryCount` - Max retry attempts (default: 5)
- `Db:MaxRetryDelaySeconds` - Max retry delay (default: 10)
- `Logging:*` - Logging configuration
- `Azure:KeyVault:Uri` - Key Vault URI (needed to bootstrap Key Vault access)
- `Azure:KeyVault:Enabled` - Enable/disable Key Vault (default: false)

## Quick Start

### Local Development (Key Vault Disabled)

1. Set `Azure:KeyVault:Enabled` to `false` in `appsettings.json`
2. Add all secrets to `appsettings.Development.json` (don't commit this file!)
3. Run the application

```json
{
  "Azure": {
    "KeyVault": {
      "Enabled": false
    }
  },
  "ConnectionStrings": {
    "IdentityDb": "Server=localhost;..."
  },
  "Jwt": {
    "SecretKey": "your-local-dev-secret-key",
    "Issuer": "https://localhost:5000",
    "Audience": "school-management-system"
  }
}
```

### Production (Key Vault Enabled)

1. Create secrets in Azure Key Vault (see [KEY_VAULT_SETUP.md](./KEY_VAULT_SETUP.md))
2. Enable Managed Identity on your Azure App Service
3. Grant Key Vault access to the Managed Identity
4. Set `Azure:KeyVault:Enabled` to `true` in production `appsettings.json`
5. Deploy the application

```json
{
  "Azure": {
    "KeyVault": {
      "Uri": "https://your-keyvault.vault.azure.net/",
      "Enabled": true
    }
  }
}
```

## Files Modified

The following files were updated to support Key Vault:

1. **Communication.cs** - Azure Communication Services credentials
2. **ServiceCollectionExtensions.cs** - JWT, Database, Health Checks
3. **JwtTokenService.cs** - JWT token generation
4. **SystemAdminSeeder.cs** - System admin seeding credentials
5. **Program.cs** - Added Communication Services registration

## Code Pattern

All configuration reads follow this pattern:

```csharp
// Try Key Vault first (--), then appsettings (:), then throw or use default
var value = configuration["Section--Key"] ?? configuration["Section:Key"]
    ?? throw new InvalidOperationException("Key not configured");
```

## Security Best Practices

✅ **DO:**
- Use Key Vault in production environments
- Enable Managed Identity for Azure-hosted apps
- Rotate secrets regularly
- Use separate Key Vaults per environment (Dev/Staging/Prod)
- Keep `appsettings.Development.json` in `.gitignore`

❌ **DON'T:**
- Commit secrets to source control
- Use the same secrets across environments
- Share Key Vault access widely
- Disable Key Vault in production

## Testing Key Vault Configuration

### Verify Key Vault is Working

1. Enable Key Vault: `"Azure:KeyVault:Enabled": true`
2. Ensure you're authenticated (Azure CLI: `az login`)
3. Run the application
4. Check logs for: "Azure Communication Service initialized successfully"
5. If errors, check Key Vault access and secret names

### Test Fallback to appsettings.json

1. Disable Key Vault: `"Azure:KeyVault:Enabled": false`
2. Add secrets to `appsettings.json` or `appsettings.Development.json`
3. Run the application
4. Verify application works without Key Vault

## Troubleshooting

| Error | Solution |
|-------|----------|
| "JWT SecretKey not configured" | Add secret to Key Vault or appsettings.json |
| "Cannot access Key Vault" | Check authentication and access policies |
| "Secret not found" | Verify secret name uses `--` in Key Vault |
| "Connection string not configured" | Add `ConnectionStrings--IdentityDb` to Key Vault |

## References

- [Azure Key Vault Setup Guide](./KEY_VAULT_SETUP.md) - Detailed setup instructions
- [Azure Key Vault Documentation](https://docs.microsoft.com/azure/key-vault/)
- [DefaultAzureCredential](https://docs.microsoft.com/dotnet/api/azure.identity.defaultazurecredential)

