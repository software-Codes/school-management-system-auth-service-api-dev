using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.Infrastructure.Persistence.Seeding;
using AuthService.Infrastructure.Persistence.Seeding.Interfaces;
using AuthService.Infrastructure.Persistence.Seeding.Seeders;
using AuthService.Infrastructure.Security;
using AuthService.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthService.IntegrationTests.Database;

/// <summary>
/// Integration tests for database seeding process
/// </summary>
public class DatabaseSeedingTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly Argon2PasswordHasher _passwordHasher;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly DateTime _testTime = new DateTime(2025, 10, 9, 12, 0, 0, DateTimeKind.Utc);

    public DatabaseSeedingTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _dateTimeProvider = new FakeDateTimeProvider(_testTime);

        var loggerMock = new Mock<ILogger<Argon2PasswordHasher>>();
        _passwordHasher = new Argon2PasswordHasher(loggerMock.Object);

        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(x => x["SeedData:SystemAdmin:Email"])
            .Returns("admin@test.local");
        _configurationMock.Setup(x => x["SeedData:SystemAdmin:TempPassword"])
            .Returns("TestAdmin123!");
    }

    [Fact]
    public async Task DatabaseSeeder_ShouldSeedAllData_InCorrectOrder()
    {
        // Arrange
        var seederService = CreateDatabaseSeederService();

        // Act
        await seederService.SeedAllAsync();

        // Assert
        var permissionsCount = await _context.Permissions.CountAsync();
        var rolesCount = await _context.Roles.CountAsync();
        var systemAdminCount = await _context.Users.CountAsync(u => u.Type == UserType.SystemAdmin);

        permissionsCount.Should().BeGreaterThan(0, "permissions should be seeded");
        rolesCount.Should().BeGreaterThan(0, "roles should be seeded");
        systemAdminCount.Should().Be(1, "system admin should be seeded");
    }

    [Fact]
    public async Task DatabaseSeeder_ShouldCreateSystemAdminRole_WithAllPermissions()
    {
        // Arrange
        var seederService = CreateDatabaseSeederService();

        // Act
        await seederService.SeedAllAsync();

        // Assert
        var systemAdminRole = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.RoleCode == "SystemAdmin");

        systemAdminRole.Should().NotBeNull();
        systemAdminRole!.RolePermissions.Should().NotBeEmpty();
        systemAdminRole.Description.Should().Contain("System Administrator");
    }

    [Fact]
    public async Task DatabaseSeeder_ShouldCreateSystemAdmin_WithCorrectCredentials()
    {
        // Arrange
        var seederService = CreateDatabaseSeederService();

        // Act
        await seederService.SeedAllAsync();

        // Assert
        var systemAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Type == UserType.SystemAdmin);

        systemAdmin.Should().NotBeNull();
        systemAdmin!.Status.Should().Be(UserStatus.Active);

        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.UserId == systemAdmin.Id);
        contact.Should().NotBeNull();
        contact!.Value.Should().Be("admin@test.local");
        contact.IsVerified.Should().BeTrue();

        var credential = await _context.Credentials
            .FirstOrDefaultAsync(c => c.UserId == systemAdmin.Id);
        credential.Should().NotBeNull();
        credential!.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task DatabaseSeeder_ShouldVerifyPassword_ForSystemAdmin()
    {
        // Arrange
        var seederService = CreateDatabaseSeederService();
        await seederService.SeedAllAsync();

        // Act
        var systemAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Type == UserType.SystemAdmin);
        var credential = await _context.Credentials
            .FirstOrDefaultAsync(c => c.UserId == systemAdmin!.Id);

        var isValidPassword = _passwordHasher.VerifyPassword("TestAdmin123!", credential!.PasswordHash);

        // Assert
        isValidPassword.Should().BeTrue("the seeded password should be verifiable");
    }

    [Fact]
    public async Task DatabaseSeeder_ShouldNotCreateDuplicates_WhenRunMultipleTimes()
    {
        // Arrange
        var seederService = CreateDatabaseSeederService();

        // Act
        await seederService.SeedAllAsync();
        await seederService.SeedAllAsync();
        await seederService.SeedAllAsync();

        // Assert
        var systemAdminCount = await _context.Users.CountAsync(u => u.Type == UserType.SystemAdmin);
        systemAdminCount.Should().Be(1, "should not create duplicate system admins");

        var rolesCount = await _context.Roles.CountAsync();
        var uniqueRoleCodes = await _context.Roles.Select(r => r.RoleCode).Distinct().CountAsync();
        rolesCount.Should().Be(uniqueRoleCodes, "should not create duplicate roles");
    }

    [Fact]
    public async Task DatabaseSeeder_ShouldCreateCommonRoles()
    {
        // Arrange
        var seederService = CreateDatabaseSeederService();

        // Act
        await seederService.SeedAllAsync();

        // Assert
        var roles = await _context.Roles.Select(r => r.RoleCode).ToListAsync();

        roles.Should().Contain("SystemAdmin");
        roles.Should().Contain("SchoolAdmin");
        roles.Should().Contain("Teacher");
        roles.Should().Contain("Parent");
        roles.Should().Contain("Student");
    }

    [Fact]
    public async Task DatabaseSeeder_ShouldCreateStandardPermissions()
    {
        // Arrange
        var seederService = CreateDatabaseSeederService();

        // Act
        await seederService.SeedAllAsync();

        // Assert
        var permissions = await _context.Permissions.Select(p => p.PermCode).ToListAsync();

        permissions.Should().NotBeEmpty();
        permissions.Should().Contain(p => p.Contains("users."));
        permissions.Should().Contain(p => p.Contains("school."));
    }

    [Fact]
    public async Task DatabaseSeeder_ShouldAssignSystemAdminRole_ToSystemAdmin()
    {
        // Arrange
        var seederService = CreateDatabaseSeederService();

        // Act
        await seederService.SeedAllAsync();

        // Assert
        var systemAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Type == UserType.SystemAdmin);

        var membership = await _context.UserSchoolMemberships
            .FirstOrDefaultAsync(m => m.UserId == systemAdmin!.Id);

        membership.Should().NotBeNull();
        
        var systemAdminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleCode == "SystemAdmin");
        membership!.RoleId.Should().Be(systemAdminRole!.Id);
        membership.Status.Should().Be(MembershipStatus.Active);
        membership.SchoolId.Should().BeNull("SystemAdmin is not tied to a school");
    }

    [Fact]
    public async Task DatabaseSeeder_ShouldSetCorrectTimestamps()
    {
        // Arrange
        var seederService = CreateDatabaseSeederService();

        // Act
        await seederService.SeedAllAsync();

        // Assert
        var systemAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Type == UserType.SystemAdmin);

        systemAdmin.Should().NotBeNull();
        systemAdmin!.CreatedAtUtc.Should().Be(_testTime);
        systemAdmin.UpdatedAtUtc.Should().BeOnOrAfter(_testTime);
    }

    [Fact]
    public async Task DatabaseSeeder_ShouldHandleConfiguration_WithMissingValues()
    {
        // Arrange
        _configurationMock.Setup(x => x["SeedData:SystemAdmin:Email"])
            .Returns((string?)null);
        _configurationMock.Setup(x => x["SeedData:SystemAdmin:TempPassword"])
            .Returns((string?)null);

        var seederService = CreateDatabaseSeederService();

        // Act
        await seederService.SeedAllAsync();

        // Assert
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Kind == ContactKind.Email);

        contact.Should().NotBeNull();
        contact!.Value.Should().Be("admin@platform.local", "should use default email");
    }

    [Fact]
    public async Task DatabaseSeeder_ShouldSeedCompleteAuthorizationModel()
    {
        // Arrange
        var seederService = CreateDatabaseSeederService();

        // Act
        await seederService.SeedAllAsync();

        // Assert - Verify complete chain: User -> Membership -> Role -> RolePermissions -> Permissions
        var systemAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Type == UserType.SystemAdmin);

        var userPermissions = await _context.UserSchoolMemberships
            .Where(m => m.UserId == systemAdmin!.Id && m.Status == MembershipStatus.Active)
            .Join(
                _context.RolePermissions,
                m => m.RoleId,
                rp => rp.RoleId,
                (m, rp) => rp.PermissionId)
            .Join(
                _context.Permissions,
                permId => permId,
                p => p.Id,
                (permId, p) => p.PermCode)
            .ToListAsync();

        userPermissions.Should().NotBeEmpty("SystemAdmin should have permissions assigned");
    }

    private DatabaseSeederService CreateDatabaseSeederService()
    {
        var adminLoggerMock = new Mock<ILogger<SystemAdminSeeder>>();
        var roleLoggerMock = new Mock<ILogger<RoleSeeder>>();
        var permLoggerMock = new Mock<ILogger<PermissionSeeder>>();
        var serviceLoggerMock = new Mock<ILogger<DatabaseSeederService>>();

        var systemAdminSeeder = new SystemAdminSeeder(
            _context,
            _dateTimeProvider,
            _passwordHasher,
            _configurationMock.Object,
            adminLoggerMock.Object);

        var roleSeeder = new RoleSeeder(_context, _dateTimeProvider, roleLoggerMock.Object);
        var permissionSeeder = new PermissionSeeder(_context, _dateTimeProvider, permLoggerMock.Object);

        return new DatabaseSeederService(
            _context,
            new IDatabaseSeeder[] { permissionSeeder, roleSeeder, systemAdminSeeder },
            serviceLoggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

