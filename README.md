# 🔐 Auth Service - School Management System

Production-ready authentication and authorization microservice for a comprehensive school management platform.

## Tech Stack

- **.NET 9.0** - Web API
- **Azure SQL Database** - Data persistence
- **Entity Framework Core 9** - ORM
- **JWT Bearer Authentication** - Token-based auth
- **Argon2id** - Password hashing (OWASP recommended)

---

## Quick Start

### 1. Run the Application
```bash
dotnet run
```

The app will:
- ✅ Connect to Azure SQL
- ✅ Apply migrations automatically
- ✅ Seed default System Admin (first time only)
- ✅ Start on `https://localhost:5001`

### 2. Login as System Admin

**POST** `https://localhost:5001/api/auth/login`

```json
{
  "email": "admin@platform.local",
  "password": "ChangeMe123!"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "...",
  "expiresIn": 900,
  "tokenType": "Bearer",
  "user": {
    "userId": "guid",
    "userType": "SystemAdmin",
    "email": "admin@platform.local",
    "permissions": ["system.admin", "school.manage", ...],
    "mustChangePassword": true
  }
}
```

---

## Architecture

### Clean Architecture Layers

```
src/
├── Abstractions/      # Interfaces (IPasswordHasher, ITokenService)
├── Domain/            # Entities, Enums, Events
├── Application/       # DTOs, Endpoints
└── Infrastructure/    # Auth, Security, Database, Seeding
```

### Key Components

| Component | Purpose |
|-----------|---------|
| **Argon2PasswordHasher** | OWASP-recommended password hashing |
| **JwtTokenService** | JWT access & refresh token generation |
| **GlobalExceptionHandler** | Centralized error handling |
| **DatabaseSeeder** | Auto-seed on first startup |

---

## Database

### Connection String
Configure in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "IdentityDb": "your-azure-sql-connection-string"
  }
}
```

### Migrations
```bash
# Create migration
dotnet ef migrations add MigrationName

# Apply to database
dotnet ef database update
```

### Schema

**9 Schemas:**
- `identity.*` - Users, Credentials, Roles, Permissions
- `school.*` - Schools, Classes
- `student.*` - Students, Enrollments
- `guardian.*` - Parent-Student links
- More to come...

---

## Security

### Password Security
- **Algorithm:** Argon2id
- **Memory Cost:** 64 MB
- **Iterations:** 4
- **Salt:** 128-bit unique per password

### Token Security
- **Access Token:** 15 minutes (JWT)
- **Refresh Token:** 30 days (hashed in DB)
- **Algorithm:** HMAC-SHA256
- **Secret:** 512-bit key

### API Security
- JWT Bearer authentication
- Role-based authorization
- Global exception handling
- RFC 7807 Problem Details responses

---

## API Endpoints

### Authentication
- `POST /api/auth/login` - System Admin login

### Diagnostics
- `GET /version` - API version
- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe
- `GET /db/test` - Database connection test

---

## Configuration

### Required Settings (appsettings.json)

```json
{
  "ConnectionStrings": {
    "IdentityDb": "your-connection-string"
  },
  "Jwt": {
    "Issuer": "https://localhost:5001",
    "Audience": "school-management-system",
    "SecretKey": "your-512-bit-secret-key",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 30
  },
  "SeedData": {
    "SystemAdmin": {
      "Email": "admin@platform.local",
      "TempPassword": "ChangeMe123!"
    }
  }
}
```

### Generate Secure JWT Secret
```bash
openssl rand -base64 64
```

---

## Development

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

### Test with Postman
1. Import `test-login.http` or use:
   - URL: `https://localhost:5001/api/auth/login`
   - Method: POST
   - Body: `{"email": "admin@platform.local", "password": "ChangeMe123!"}`
2. Disable SSL verification (dev only)
3. Send request → receive tokens

---

## Project Structure

```
auth-service/
├── src/
│   ├── Abstractions/
│   ├── Domain/
│   ├── Application/
│   └── Infrastructure/
├── appsettings.json
├── Program.cs
├── README.md
└── test-login.http
```

---

## Design Principles

**SOLID:**
- ✅ Single Responsibility
- ✅ Open/Closed
- ✅ Liskov Substitution
- ✅ Interface Segregation
- ✅ Dependency Inversion

**CUPID:**
- ✅ Composable
- ✅ Unix Philosophy
- ✅ Predictable
- ✅ Idempotent
- ✅ Domain-based

---

## License

© 2025 School Management System. All Rights Reserved.

---

## Quick Reference

**Default Credentials:**  
Email: `admin@platform.local`  
Password: `ChangeMe123!`

**Login Endpoint:**  
`POST https://localhost:5001/api/auth/login`

**Token Lifespan:**  
Access: 15 min | Refresh: 30 days

**Status:** ✅ Production Ready

## Testing

### Interactive Test Runner
```bash
./test-runner.sh
```

### Manual Test Commands
```bash
# Unit tests
dotnet test src/tests/unit/AuthService.UnitTests.csproj

# Integration tests
dotnet test src/tests/integration/AuthService.IntegrationTests.csproj

# All tests with detailed logging
dotnet test --verbosity detailed --logger "console;verbosity=detailed"

# Feature-specific tests
dotnet test --filter "School" --verbosity detailed
dotnet test --filter "SystemAdmin" --verbosity detailed
```