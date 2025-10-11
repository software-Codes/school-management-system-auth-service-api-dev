# Azure Key Vault Configuration

This document describes all the secrets that should be stored in Azure Key Vault for the Auth Service.

## Configuration Overview

The application is configured to read secrets from Azure Key Vault when enabled. If Key Vault is disabled or a secret is not found, it falls back to `appsettings.json`.

### Enabling Key Vault

In `appsettings.json`:
```json
{
  "Azure": {
    "KeyVault": {
      "Uri": "https://your-keyvault-name.vault.azure.net/",
      "Enabled": true
    }
  }
}
```

## Required Secrets in Azure Key Vault

All secrets in Azure Key Vault use `--` (double dash) as the hierarchical separator instead of `:` (colon).

### 1. Database Connection String
**Key Vault Secret Name:** `ConnectionStrings--IdentityDb`  
**Description:** SQL Server connection string for the Identity database  
**Example Value:**
```
Server=tcp:your-server.database.windows.net,1433;Initial Catalog=IdentityDb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication='Active Directory Default';
```

### 2. JWT Configuration

**Key Vault Secret Name:** `Jwt--SecretKey`  
**Description:** Secret key for signing JWT tokens (minimum 256-bit key)  
**Example Value:**
```
your-super-secure-secret-key-at-least-32-characters-long-base64-encoded
```

**Key Vault Secret Name:** `Jwt--Issuer`  
**Description:** JWT token issuer  
**Example Value:**
```
https://your-auth-service.com
```

**Key Vault Secret Name:** `Jwt--Audience`  
**Description:** JWT token audience  
**Example Value:**
```
school-management-system
```

### 3. Azure Communication Services

**Key Vault Secret Name:** `AzureCommunication--ConnectionString`  
**Description:** Azure Communication Services connection string  
**Example Value:**
```
endpoint=https://your-acs-resource.communication.azure.com/;accesskey=your-access-key
```

**Key Vault Secret Name:** `AzureCommunication--FromPhoneNumber`  
**Description:** Phone number for sending SMS (must be provisioned in ACS)  
**Example Value:**
```
+12345678900
```

**Key Vault Secret Name:** `AzureCommunication--FromEmailAddress`  
**Description:** Email address for sending emails (must be verified in ACS)  
**Example Value:**
```
noreply@yourdomain.com
```

### 4. System Admin Seed Data (Optional)

**Key Vault Secret Name:** `SeedData--SystemAdmin--Email`  
**Description:** Email address for the default system administrator  
**Example Value:**
```
admin@platform.local
```

**Key Vault Secret Name:** `SeedData--SystemAdmin--TempPassword`  
**Description:** Temporary password for system admin (should be changed on first login)  
**Example Value:**
```
ChangeMe123!
```
**Security Note:** This should be a strong temporary password that must be changed immediately after first login.

### 5. Redis Configuration (Future Use)

**Key Vault Secret Name:** `Redis--Connection`  
**Description:** Redis connection string for caching  
**Example Value:**
```
your-redis-instance.redis.cache.windows.net:6380,password=your-redis-key,ssl=True,abortConnect=False
```

### 6. Service Bus Configuration (Future Use)

**Key Vault Secret Name:** `ServiceBus--Connection`  
**Description:** Azure Service Bus connection string for event publishing  
**Example Value:**
```
Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key
```

**Key Vault Secret Name:** `ServiceBus--TopicName`  
**Description:** Service Bus topic name for identity events  
**Example Value:**
```
identity-events
```

## Setting Secrets in Azure Key Vault

### Using Azure CLI

```bash
# Login to Azure
az login

# Set Key Vault name
KEYVAULT_NAME="your-keyvault-name"

# Database Connection String
az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "ConnectionStrings--IdentityDb" \
  --value "Server=tcp:your-server.database.windows.net,1433;..."

# JWT Secret Key
az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "Jwt--SecretKey" \
  --value "your-super-secure-secret-key"

# JWT Issuer
az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "Jwt--Issuer" \
  --value "https://your-auth-service.com"

# JWT Audience
az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "Jwt--Audience" \
  --value "school-management-system"

# Azure Communication Services
az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "AzureCommunication--ConnectionString" \
  --value "endpoint=https://your-acs.communication.azure.com/;..."

az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "AzureCommunication--FromPhoneNumber" \
  --value "+12345678900"

az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "AzureCommunication--FromEmailAddress" \
  --value "noreply@yourdomain.com"

# System Admin Seed Data
az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "SeedData--SystemAdmin--Email" \
  --value "admin@platform.local"

az keyvault secret set --vault-name $KEYVAULT_NAME \
  --name "SeedData--SystemAdmin--TempPassword" \
  --value "YourStrongTempPassword123!"
```

### Using Azure Portal

1. Navigate to your Key Vault in the Azure Portal
2. Go to **Secrets** in the left menu
3. Click **+ Generate/Import**
4. Enter the secret name (e.g., `ConnectionStrings--IdentityDb`)
5. Enter the secret value
6. Click **Create**

## Authentication to Key Vault

The application uses `DefaultAzureCredential` which supports multiple authentication methods in order:

1. **Environment Variables** - Service Principal credentials
2. **Managed Identity** - For Azure-hosted applications (App Service, Functions, VMs)
3. **Visual Studio** - For local development
4. **Azure CLI** - For local development
5. **Azure PowerShell** - For local development

### For Local Development

Login using Azure CLI:
```bash
az login
```

### For Production (Azure App Service)

1. Enable System-assigned Managed Identity on your App Service
2. Grant the Managed Identity access to Key Vault:

```bash
# Get the App Service Managed Identity Object ID
APP_IDENTITY=$(az webapp identity show --name your-app-name --resource-group your-rg --query principalId -o tsv)

# Grant Key Vault access
az keyvault set-policy --name your-keyvault-name \
  --object-id $APP_IDENTITY \
  --secret-permissions get list
```

## Fallback to appsettings.json

For local development, you can disable Key Vault and use `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Azure": {
    "KeyVault": {
      "Enabled": false
    }
  },
  "ConnectionStrings": {
    "IdentityDb": "Server=localhost;Database=IdentityDb;..."
  },
  "Jwt": {
    "SecretKey": "local-dev-secret-key",
    "Issuer": "https://localhost:5000",
    "Audience": "school-management-system"
  }
}
```

## Security Best Practices

1. **Never commit secrets to source control** - Use `.gitignore` for `appsettings.*.json` files with secrets
2. **Rotate secrets regularly** - Update Key Vault secrets periodically
3. **Use separate Key Vaults per environment** - Dev, Staging, Production
4. **Limit access** - Use RBAC and access policies to restrict who can read secrets
5. **Enable audit logging** - Monitor Key Vault access in Azure Monitor
6. **Use Managed Identity in production** - Avoid storing credentials for Key Vault access

## Troubleshooting

### Application can't access Key Vault

Check:
1. Is Key Vault enabled in configuration?
2. Is the Key Vault URI correct?
3. Does the application identity have access to Key Vault?
4. Are you authenticated locally (for development)?

### Secret not found

Check:
1. Is the secret name correct (use `--` not `:`)?
2. Does the secret exist in Key Vault?
3. Does the application have `Get` permission on secrets?

### Local development authentication issues

```bash
# Login to Azure CLI
az login

# Verify your identity
az account show

# Test Key Vault access
az keyvault secret show --vault-name your-keyvault-name --name "Jwt--SecretKey"
```

