using AuthService.Abstractions.Common;
using AuthService.Abstractions.Security;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.Infrastructure.Persistence.Seeding.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Persistence.Seeding.Seeders;

/// <summary>
/// Seeds the initial System Administrator user
/// Single Responsibility: Only handles system admin user creation
/// </summary>
public sealed class SystemAdminSeeder : IDatabaseSeeder
{
    private readonly IdentityDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemAdminSeeder> _logger;

    public SystemAdminSeeder(
        IdentityDbContext context,
        IDateTimeProvider dateTimeProvider,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<SystemAdminSeeder> logger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting System Admin seeding...");

        var adminEmail = _configuration["SeedData:SystemAdmin:Email"] ?? "admin@platform.local";
        var now = _dateTimeProvider.UtcNow;

        var existingAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Type == UserType.SystemAdmin, cancellationToken);

        if (existingAdmin != null)
        {
            _logger.LogInformation("System Admin already exists with ID: {UserId}", existingAdmin.Id);
            return;
        }

        var systemAdminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleCode == "SystemAdmin", cancellationToken);

        if (systemAdminRole == null)
        {
            _logger.LogError("SystemAdmin role not found. Please run role seeding first.");
            throw new InvalidOperationException("SystemAdmin role not found. Cannot create system admin user.");
        }

        var user = User.Create(UserType.SystemAdmin, now);
        user.Activate(now);
        
        await _context.Users.AddAsync(user, cancellationToken);
        _logger.LogInformation("Created System Admin user with ID: {UserId}", user.Id);

        var contact = Contact.Create(user.Id, ContactKind.Email, adminEmail, isPrimary: true, now);
        contact.MarkAsVerified(now); 
        
        await _context.Contacts.AddAsync(contact, cancellationToken);
        _logger.LogInformation("Created contact for System Admin: {Email}", adminEmail);

        // Create credential with a temporary password using Argon2id
        // NOTE: In production, this should be changed immediately after first login
        var tempPassword = _configuration["SeedData:SystemAdmin:TempPassword"] ?? "ChangeMe123!";
        var passwordHash = _passwordHasher.HashPassword(tempPassword);
        
        var credential = Credential.Create(
            user.Id,
            passwordHash,
            MfaMode.PasswordAndOtp,
            mustChangePassword: true, // Force password change on first login
            now);
        
        await _context.Credentials.AddAsync(credential, cancellationToken);
        _logger.LogWarning("Created temporary password for System Admin using Argon2id. MUST BE CHANGED on first login!");

        var membership = UserSchoolMembership.Create(
            user.Id,
            schoolId: null,
            systemAdminRole.Id,
            now);
        membership.Activate(now);
        
        await _context.UserSchoolMemberships.AddAsync(membership, cancellationToken);
        _logger.LogInformation("Assigned SystemAdmin role to user");

        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("System Admin seeding completed successfully!");
        _logger.LogWarning("IMPORTANT: Default credentials - Email: {Email}, Password: {Password} (MUST CHANGE!)", 
            adminEmail, tempPassword);
    }
}

