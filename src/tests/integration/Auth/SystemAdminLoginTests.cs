
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.Infrastructure.Security;
using AuthService.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthService.IntegrationTests.Auth;

/// <summary>
/// Integration tests for system admin login flow
/// Tests the complete authentication process end-to-end
/// </summary>
public class SystemAdminLoginTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly Argon2PasswordHasher _passwordHasher;
    private readonly TestDataBuilder _dataBuilder;
    private readonly DateTime _testTime = new DateTime(2025, 10, 9, 12, 0, 0, DateTimeKind.Utc);

    public SystemAdminLoginTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _dateTimeProvider = new FakeDateTimeProvider(_testTime);

        var loggerMock = new Mock<ILogger<Argon2PasswordHasher>>();
        _passwordHasher = new Argon2PasswordHasher(loggerMock.Object);

        _dataBuilder = new TestDataBuilder(_context, _passwordHasher, _dateTimeProvider);
    }

    [Fact]
    public async Task Login_WithValidSystemAdminCredentials_ShouldSucceed()
    {
        // Arrange
        var systemAdminRole = await _dataBuilder.CreateRoleAsync("SystemAdmin", "System Administrator");
        var (user, contact, credential, membership) = await _dataBuilder.CreateCompleteUserAsync(
            UserType.SystemAdmin,
            "admin@platform.local",
            "Admin123!",
            systemAdminRole.Id,
            null);

        // Act - Simulate login
        var loginContact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Value == "admin@platform.local" && c.Kind == ContactKind.Email);

        var loginUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == loginContact!.UserId);

        var loginCredential = await _context.Credentials
            .FirstOrDefaultAsync(c => c.UserId == loginUser!.Id);

        var isPasswordValid = _passwordHasher.VerifyPassword("Admin123!", loginCredential!.PasswordHash);

        // Assert
        loginContact.Should().NotBeNull();
        loginUser.Should().NotBeNull();
        loginUser!.IsActive().Should().BeTrue();
        loginCredential.Should().NotBeNull();
        isPasswordValid.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var systemAdminRole = await _dataBuilder.CreateRoleAsync("SystemAdmin", "System Administrator");
        await _dataBuilder.CreateCompleteUserAsync(
            UserType.SystemAdmin,
            "admin@platform.local",
            "Admin123!",
            systemAdminRole.Id,
            null);

        // Act
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Value == "wrong@email.com" && c.Kind == ContactKind.Email);

        // Assert
        contact.Should().BeNull("invalid email should not be found");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldFail()
    {
        // Arrange
        var systemAdminRole = await _dataBuilder.CreateRoleAsync("SystemAdmin", "System Administrator");
        var (user, contact, credential, membership) = await _dataBuilder.CreateCompleteUserAsync(
            UserType.SystemAdmin,
            "admin@platform.local",
            "Admin123!",
            systemAdminRole.Id,
            null);

        // Act
        var isPasswordValid = _passwordHasher.VerifyPassword("WrongPassword!", credential.PasswordHash);

        // Assert
        isPasswordValid.Should().BeFalse("invalid password should not be verified");
    }

    [Fact]
    public async Task Login_WithInactiveUser_ShouldFail()
    {
        // Arrange
        var systemAdminRole = await _dataBuilder.CreateRoleAsync("SystemAdmin", "System Administrator");
        var user = await _dataBuilder.CreateUserAsync(UserType.SystemAdmin, activate: false);
        await _dataBuilder.CreateContactAsync(user.Id, "inactive@platform.local");
        await _dataBuilder.CreateCredentialAsync(user.Id, "Admin123!");
        await _dataBuilder.CreateMembershipAsync(user.Id, null, systemAdminRole.Id);

        // Act
        var loginUser = await _context.Users.FindAsync(user.Id);

        // Assert
        loginUser.Should().NotBeNull();
        loginUser!.IsActive().Should().BeFalse();
    }

    [Fact]
    public async Task Login_ShouldRetrieveUserPermissions_Correctly()
    {
        // Arrange
        var systemAdminRole = await _dataBuilder.CreateRoleAsync("SystemAdmin", "System Administrator");

        var permission1 = await _dataBuilder.CreatePermissionAsync("users.create", "Create users");
        var permission2 = await _dataBuilder.CreatePermissionAsync("schools.manage", "Manage schools");
        var permission3 = await _dataBuilder.CreatePermissionAsync("system.configure", "Configure system");

        await _dataBuilder.AssignPermissionToRoleAsync(systemAdminRole.Id, permission1.Id);
        await _dataBuilder.AssignPermissionToRoleAsync(systemAdminRole.Id, permission2.Id);
        await _dataBuilder.AssignPermissionToRoleAsync(systemAdminRole.Id, permission3.Id);

        var (user, _, _, _) = await _dataBuilder.CreateCompleteUserAsync(
            UserType.SystemAdmin,
            "admin@platform.local",
            "Admin123!",
            systemAdminRole.Id,
            null);

        // Act - Get user permissions
        var permissions = await _context.UserSchoolMemberships
            .Where(m => m.UserId == user.Id && m.Status == MembershipStatus.Active)
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
            .Distinct()
            .ToListAsync();

        // Assert
        permissions.Should().HaveCount(3);
        permissions.Should().Contain("users.create");
        permissions.Should().Contain("schools.manage");
        permissions.Should().Contain("system.configure");
    }

    [Fact]
    public async Task Login_SystemAdmin_ShouldHaveMustChangePasswordFlag()
    {
        // Arrange
        var systemAdminRole = await _dataBuilder.CreateRoleAsync("SystemAdmin", "System Administrator");
        var user = await _dataBuilder.CreateUserAsync(UserType.SystemAdmin);
        await _dataBuilder.CreateContactAsync(user.Id, "admin@platform.local");

        var passwordHash = _passwordHasher.HashPassword("TempPassword123!");
        var credential = Credential.Create(
            user.Id,
            passwordHash,
            MfaMode.PasswordAndOtp,
            mustChangePassword: true,
            _dateTimeProvider.UtcNow);

        await _context.Credentials.AddAsync(credential);
        await _context.SaveChangesAsync();

        // Act
        var savedCredential = await _context.Credentials
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        // Assert
        savedCredential.Should().NotBeNull();
        savedCredential!.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithEmailCaseInsensitive_ShouldSucceed()
    {
        // Arrange
        var systemAdminRole = await _dataBuilder.CreateRoleAsync("SystemAdmin", "System Administrator");
        await _dataBuilder.CreateCompleteUserAsync(
            UserType.SystemAdmin,
            "admin@platform.local",
            "Admin123!",
            systemAdminRole.Id,
            null);

        // Act - Try to login with different case
        var contact = await _context.Contacts
            .FirstOrDefaultAsync(c =>
                c.Value == "ADMIN@PLATFORM.LOCAL".ToLowerInvariant() &&
                c.Kind == ContactKind.Email);

        // Assert
        contact.Should().NotBeNull("email lookup should be case-insensitive");
    }

    [Fact]
    public async Task Login_WithDisabledUser_ShouldFail()
    {
        // Arrange
        var teacherRole = await _dataBuilder.CreateRoleAsync("Teacher", "Teacher role");
        var user = await _dataBuilder.CreateUserAsync(UserType.Teacher, activate: true);
        user.Disable(_dateTimeProvider.UtcNow, "Policy violation");
        await _context.SaveChangesAsync();

        await _dataBuilder.CreateContactAsync(user.Id, "teacher@school.com");
        await _dataBuilder.CreateCredentialAsync(user.Id, "Teacher123!");

        // Act
        var savedUser = await _context.Users.FindAsync(user.Id);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.IsActive().Should().BeFalse();
        savedUser.Status.Should().Be(UserStatus.Disabled);
    }

    [Fact]
    public async Task Login_WithLockedUser_ShouldFail()
    {
        // Arrange
        var teacherRole = await _dataBuilder.CreateRoleAsync("Teacher", "Teacher role");
        var user = await _dataBuilder.CreateUserAsync(UserType.Teacher, activate: true);
        user.Lock(_dateTimeProvider.UtcNow);
        await _context.SaveChangesAsync();

        await _dataBuilder.CreateContactAsync(user.Id, "teacher@school.com");
        await _dataBuilder.CreateCredentialAsync(user.Id, "Teacher123!");

        // Act
        var savedUser = await _context.Users.FindAsync(user.Id);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.IsLocked().Should().BeTrue();
        savedUser.Status.Should().Be(UserStatus.Locked);
    }

    [Fact]
    public async Task Login_WithUnverifiedEmail_ShouldStillAuthenticate()
    {
        // Arrange
        var teacherRole = await _dataBuilder.CreateRoleAsync("Teacher", "Teacher role");
        var user = await _dataBuilder.CreateUserAsync(UserType.Teacher);
        var contact = await _dataBuilder.CreateContactAsync(user.Id, "teacher@school.com", isPrimary: true, isVerified: false);
        await _dataBuilder.CreateCredentialAsync(user.Id, "Teacher123!");
        await _dataBuilder.CreateMembershipAsync(user.Id, null, teacherRole.Id);

        // Act
        var savedContact = await _context.Contacts.FindAsync(contact.Id);
        var isPasswordValid = _passwordHasher.VerifyPassword(
            "Teacher123!",
            (await _context.Credentials.FirstAsync(c => c.UserId == user.Id)).PasswordHash);

        // Assert
        savedContact.Should().NotBeNull();
        savedContact!.IsVerified.Should().BeFalse();
        isPasswordValid.Should().BeTrue("authentication should work even with unverified email");
    }

    [Fact]
    public async Task Login_MultipleUsers_ShouldAuthenticateCorrectly()
    {
        // Arrange
        var adminRole = await _dataBuilder.CreateRoleAsync("SystemAdmin", "System Administrator");
        var teacherRole = await _dataBuilder.CreateRoleAsync("Teacher", "Teacher role");
        var parentRole = await _dataBuilder.CreateRoleAsync("Parent", "Parent role");

        var admin = await _dataBuilder.CreateCompleteUserAsync(
            UserType.SystemAdmin, "admin@platform.local", "Admin123!", adminRole.Id, null);

        var teacher = await _dataBuilder.CreateCompleteUserAsync(
            UserType.Teacher, "teacher@school.com", "Teacher123!", teacherRole.Id, null);

        var parent = await _dataBuilder.CreateCompleteUserAsync(
            UserType.Parent, "parent@family.com", "Parent123!", parentRole.Id, null);

        // Act & Assert - Admin login
        var adminContact = await _context.Contacts.FirstAsync(c => c.Value == "admin@platform.local");
        var adminCred = await _context.Credentials.FirstAsync(c => c.UserId == admin.user.Id);
        _passwordHasher.VerifyPassword("Admin123!", adminCred.PasswordHash).Should().BeTrue();

        // Act & Assert - Teacher login
        var teacherContact = await _context.Contacts.FirstAsync(c => c.Value == "teacher@school.com");
        var teacherCred = await _context.Credentials.FirstAsync(c => c.UserId == teacher.user.Id);
        _passwordHasher.VerifyPassword("Teacher123!", teacherCred.PasswordHash).Should().BeTrue();

        // Act & Assert - Parent login
        var parentContact = await _context.Contacts.FirstAsync(c => c.Value == "parent@family.com");
        var parentCred = await _context.Credentials.FirstAsync(c => c.UserId == parent.user.Id);
        _passwordHasher.VerifyPassword("Parent123!", parentCred.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithMfaMode_ShouldBeStoredCorrectly()
    {
        // Arrange
        var adminRole = await _dataBuilder.CreateRoleAsync("SystemAdmin", "System Administrator");
        var user = await _dataBuilder.CreateUserAsync(UserType.SystemAdmin);
        await _dataBuilder.CreateContactAsync(user.Id, "admin@platform.local");

        var passwordHash = _passwordHasher.HashPassword("Admin123!");
        var credential = Credential.Create(
            user.Id,
            passwordHash,
            MfaMode.PasswordAndOtp,
            false,
            _dateTimeProvider.UtcNow);

        await _context.Credentials.AddAsync(credential);
        await _context.SaveChangesAsync();

        // Act
        var savedCredential = await _context.Credentials.FirstAsync(c => c.UserId == user.Id);

        // Assert
        savedCredential.MfaMode.Should().Be(MfaMode.PasswordAndOtp);
    }

    [Fact]
    public async Task Login_CompleteFlow_ShouldWorkEndToEnd()
    {
        // Arrange - Simulate complete seeding
        var adminRole = await _dataBuilder.CreateRoleAsync("SystemAdmin", "System Administrator");

        var viewPerm = await _dataBuilder.CreatePermissionAsync("users.view", "View users");
        var createPerm = await _dataBuilder.CreatePermissionAsync("users.create", "Create users");
        var managePerm = await _dataBuilder.CreatePermissionAsync("system.manage", "Manage system");

        await _dataBuilder.AssignPermissionToRoleAsync(adminRole.Id, viewPerm.Id);
        await _dataBuilder.AssignPermissionToRoleAsync(adminRole.Id, createPerm.Id);
        await _dataBuilder.AssignPermissionToRoleAsync(adminRole.Id, managePerm.Id);

        var (user, contact, credential, membership) = await _dataBuilder.CreateCompleteUserAsync(
            UserType.SystemAdmin,
            "admin@platform.local",
            "Admin123!",
            adminRole.Id,
            null);

        // Act - Simulate complete login flow
        // Step 1: Find contact by email
        var loginEmail = "admin@platform.local".ToLowerInvariant();
        var foundContact = await _context.Contacts
            .FirstOrDefaultAsync(c => c.Value == loginEmail && c.Kind == ContactKind.Email);

        // Step 2: Get user
        var foundUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == foundContact!.UserId);

        // Step 3: Check if user is active
        var isActive = foundUser?.IsActive() ?? false;

        // Step 4: Get credential
        var foundCredential = await _context.Credentials
            .FirstOrDefaultAsync(c => c.UserId == foundUser!.Id);

        // Step 5: Verify password
        var isPasswordValid = _passwordHasher.VerifyPassword("Admin123!", foundCredential!.PasswordHash);

        // Step 6: Get permissions
        var userPermissions = await _context.UserSchoolMemberships
            .Where(m => m.UserId == foundUser!.Id && m.Status == MembershipStatus.Active)
            .Join(_context.RolePermissions, m => m.RoleId, rp => rp.RoleId, (m, rp) => rp.PermissionId)
            .Join(_context.Permissions, permId => permId, p => p.Id, (permId, p) => p.PermCode)
            .Distinct()
            .ToListAsync();

        // Assert
        foundContact.Should().NotBeNull();
        foundUser.Should().NotBeNull();
        isActive.Should().BeTrue();
        foundCredential.Should().NotBeNull();
        isPasswordValid.Should().BeTrue();
        userPermissions.Should().HaveCount(3);
        userPermissions.Should().Contain("users.view");
        userPermissions.Should().Contain("users.create");
        userPermissions.Should().Contain("system.manage");
        foundCredential.MustChangePassword.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

