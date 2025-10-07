auth-service/
├─ README.md
├─ AuthService.sln
├─ /src
│  ├─ /Abstractions/                   # Thin contracts shared across layers
│  │  ├─ AuthService.Abstractions.csproj
│  │  ├─ /Common/                      # Generic primitives (Results, Errors, Paged)
│  │  ├─ /Bus/                         # Event contracts (UserCreated, TokenRevoked…)
│  │  ├─ /Auth/                        # Auth contracts (ITokenService, IPasswordHasher…)
│  │  ├─ /Otp/                         # IOtpSender, IOtpVerifier
│  │  ├─ /Notifications/               # INotificationService (wrapper over Twilio/email)
│  │  ├─ /Time/                        # IDateTimeProvider
│  │  └─ /Security/                    # ICurrentUser, IPermissionChecker
│  │
│  ├─ /Api/                            # Host (minimal APIs/Controllers, DI, filters)
│  │  ├─ AuthService.Api.csproj
│  │  ├─ Program.cs                    # composition root (no business logic)
│  │  ├─ /Configuration/               # host config binding, options validation
│  │  ├─ /Endpoints/                   # HTTP surfaces grouped by feature
│  │  │  ├─ /Auth/                     # /auth/login, /auth/otp, /auth/refresh...
│  │  │  ├─ /Users/                    # /users (admin/system only)
│  │  │  ├─ /Memberships/              # /memberships
│  │  │  └─ /Diagnostics/              # health, readiness, version
│  │  ├─ /Contracts/                   # Request/Response DTOs ONLY for the API surface
│  │  ├─ /Filters/                     # Exception/validation filters, problem details
│  │  ├─ /Auth/                        # JWT bearer setup, policies, authorization handlers
│  │  ├─ /Mapping/                     # API ↔ Application mappers (no domain refs)
│  │  ├─ /Middlewares/                 # CorrelationId, audit context, rate-limit (if any)
│  │  └─ /OpenApi/                     # Swagger/NSwag, examples, grouping
│  │
│  ├─ /Application/                    # Use cases, orchestration, policies (pure)
│  │  ├─ AuthService.Application.csproj
│  │  ├─ /Common/                      # behaviors (validation, logging), base query/command
│  │  ├─ /Interfaces/                  # Ports to Infrastructure (repositories, services)
│  │  ├─ /Auth/                        # CQRS handlers for login, refresh, logout, revoke
│  │  │  ├─ /Commands/
│  │  │  ├─ /Queries/
│  │  │  └─ /Policies/                 # MFA policies, lockout policy, token TTL rules
│  │  ├─ /Otp/                         # Issue/verify flows, rate-limit, challenges
│  │  ├─ /Users/                       # Create user, set password, change password
│  │  ├─ /Memberships/                 # Assign/disable roles per school (claims shaping)
│  │  ├─ /Events/                      # Publishing app events (UserCreated…)
│  │  ├─ /Validation/                  # Fluent validators for commands/queries
│  │  ├─ /Mapping/                     # Application ↔ Domain mappers
│  │  └─ /Models/                      # Application-level result models (not EF entities)
│  │
│  ├─ /Domain/                         # Enterprise core (entities, values, rules)
│  │  ├─ AuthService.Domain.csproj
│  │  ├─ /Entities/
│  │  │  ├─ User.cs
│  │  │  ├─ Contact.cs
│  │  │  ├─ Credential.cs
│  │  │  ├─ Username.cs
│  │  │  ├─ Membership.cs
│  │  │  ├─ RefreshToken.cs
│  │  │  └─ Audit.cs
│  │  ├─ /ValueObjects/
│  │  │  ├─ UserId.cs, SchoolId.cs, RoleId.cs, ContactValue.cs, UsernameValue.cs
│  │  ├─ /Aggregates/
│  │  │  └─ IdentityAggregate.cs       # if you prefer aggregate boundaries
│  │  ├─ /Enums/                       # UserType, Status, MfaMode
│  │  ├─ /Events/                      # Domain events (PasswordChanged, LoginFailed…)
│  │  ├─ /Errors/                      # Domain-specific error codes
│  │  └─ /Specifications/              # Domain specs (e.g., active membership by school)
│  │
│  └─ /Infrastructure/                 # Adapters (DB, Bus, Twilio, Redis, JWT)
│     ├─ AuthService.Infrastructure.csproj
│     ├─ /Persistence/
│     │  ├─ /EfCore/                   # DbContext, entity configs, migrations
│     │  ├─ /Repositories/             # IUserRepository, IMembershipRepository impls
│     │  └─ /Queries/                  # Dapper-based read models (optional)
│     ├─ /Security/
│     │  ├─ TokenService               # JWT/refresh issuance & validation
│     │  ├─ PasswordHasher             # Argon2/bcrypt adapter
│     │  └─ TotpProvider               # TOTP generation/verification
│     ├─ /Otp/
│     │  ├─ TwilioOtpSender            # Twilio Verify integration
│     │  └─ OtpStore                   # Redis keys for attempts/cooldowns (ephemeral)
│     ├─ /Bus/
│     │  ├─ ServiceBusPublisher        # Azure Service Bus (topics)
│     │  └─ OutboxProcessor            # outbox → bus relay (hosted service)
│     ├─ /Caching/
│     │  └─ RedisCache                 # IDistributedCache wrappers, token deny-list
│     ├─ /Clock/ 
│     │  └─ SystemDateTimeProvider
│     ├─ /Configuration/               # Options classes, validation, key vault bindings
│     ├─ /HealthChecks/                # DB, Redis, Service Bus, Twilio
│     └─ /Logging/                     # Serilog/AIK sinks & enricher config
│
├─ /tests
│  ├─ /Unit/
│  │  ├─ AuthService.Domain.Tests/
│  │  └─ AuthService.Application.Tests/
│  ├─ /Integration/
│  │  ├─ AuthService.Infrastructure.Tests/   # EF Core, Repos, Outbox
│  │  └─ AuthService.Api.Tests/              # API contract tests
│  └─ /Contract/
│     └─ AuthService.Api.ContractTests/      # HTTP contract, examples, snapshots
│
├─ /deploy
│  ├─ appsettings.Template.json              # baseline config (no secrets)
│  ├─ bicep/terraform/                       # infra-as-code (optional)
│  ├─ Dockerfile
│  ├─ docker-compose.yml                     # local dev: sqlserver, redis, azurite
│  └─ k8s/                                   # manifests/helm (if using AKS)
└─ /tools
   ├─ scripts/                               # db-migrate, seed, rotate-keys
   └─ hooks/                                 # pre-commit checks, codegen stubs
