using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthService.IntegrationTests.Database;

/// <summary>
/// Integration tests for database configuration and Entity Framework setup
/// </summary>
public class DatabaseConfigurationTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly DateTime _testTime = new DateTime(2025, 10, 9, 12, 0, 0, DateTimeKind.Utc);

    public DatabaseConfigurationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
    }

    [Fact]
    public async Task Database_ShouldBeCreated_WhenContextIsInitialized()
    {
        // Act
        var canConnect = await _context.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task Database_ShouldPersistUser_WithCorrectSchema()
    {
        // Arrange
        var user = User.Create(UserType.SystemAdmin, _testTime);

        // Act
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FindAsync(user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Id.Should().Be(user.Id);
        savedUser.Type.Should().Be(UserType.SystemAdmin);
        savedUser.Status.Should().Be(UserStatus.Pending);
    }

    [Fact]
    public async Task Database_ShouldEnforceUniqueConstraint_OnContactEmailPerUser()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var contact1 = Contact.Create(user.Id, ContactKind.Email, "test@school.com", true, _testTime);
        await _context.Contacts.AddAsync(contact1);
        await _context.SaveChangesAsync();

        // Act - Try to add duplicate contact for same user
        var contact2 = Contact.Create(user.Id, ContactKind.Email, "test@school.com", false, _testTime);
        await _context.Contacts.AddAsync(contact2);

        Func<Task> act = async () => await _context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Database_ShouldEnforceUniqueConstraint_OnRoleCode()
    {
        // Arrange
        var role1 = Role.Create("Teacher", "Teacher role", _testTime);
        await _context.Roles.AddAsync(role1);
        await _context.SaveChangesAsync();

        // Act - Try to add role with same code
        var role2 = Role.Create("Teacher", "Another teacher role", _testTime);
        await _context.Roles.AddAsync(role2);

        Func<Task> act = async () => await _context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Database_ShouldEnforceUniqueConstraint_OnPermissionCode()
    {
        // Arrange
        var perm1 = Permission.Create("users.create", "Create users", _testTime);
        await _context.Permissions.AddAsync(perm1);
        await _context.SaveChangesAsync();

        // Act - Try to add permission with same code
        var perm2 = Permission.Create("users.create", "Create new users", _testTime);
        await _context.Permissions.AddAsync(perm2);

        Func<Task> act = async () => await _context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Database_ShouldStorePasswordHash_AsBinary()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var passwordHash = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var credential = Credential.Create(user.Id, passwordHash, MfaMode.PasswordOnly, false, _testTime);

        // Act
        await _context.Credentials.AddAsync(credential);
        await _context.SaveChangesAsync();

        // Assert
        var savedCredential = await _context.Credentials.FirstAsync(c => c.UserId == user.Id);
        savedCredential.PasswordHash.Should().BeEquivalentTo(passwordHash);
    }

    [Fact]
    public async Task Database_ShouldSupportNullableSchoolId_ForSystemAdmin()
    {
        // Arrange
        var user = User.Create(UserType.SystemAdmin, _testTime);
        await _context.Users.AddAsync(user);

        var role = Role.Create("SystemAdmin", "System Administrator", _testTime);
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        // Act - Create membership without schoolId
        var membership = UserSchoolMembership.Create(user.Id, null, role.Id, _testTime);
        await _context.UserSchoolMemberships.AddAsync(membership);
        await _context.SaveChangesAsync();

        // Assert
        var savedMembership = await _context.UserSchoolMemberships.FirstAsync(m => m.UserId == user.Id);
        savedMembership.SchoolId.Should().BeNull();
    }

    [Fact]
    public async Task Database_ShouldPersistEnumsAsStrings()
    {
        // Arrange
        var user = User.Create(UserType.Parent, _testTime);
        user.Activate(_testTime);

        // Act
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Clear change tracker and reload
        _context.ChangeTracker.Clear();
        var savedUser = await _context.Users.FindAsync(user.Id);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.Type.Should().Be(UserType.Parent);
        savedUser.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task Database_ShouldSupportCascadeDelete_ForRolePermissions()
    {
        // Arrange
        var role = Role.Create("TestRole", "Test role", _testTime);
        var permission = Permission.Create("test.permission", "Test permission", _testTime);
        await _context.Roles.AddAsync(role);
        await _context.Permissions.AddAsync(permission);
        await _context.SaveChangesAsync();

        var rolePermission = RolePermission.Create(role.Id, permission.Id, _testTime);
        await _context.RolePermissions.AddAsync(rolePermission);
        await _context.SaveChangesAsync();

        // Act - Delete role should cascade to role permissions
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        // Assert
        var remainingRolePermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync();
        remainingRolePermissions.Should().BeEmpty();
    }

    [Fact]
    public async Task Database_ShouldStoreTimestamps_WithMillisecondPrecision()
    {
        // Arrange
        var precisTime = new DateTime(2025, 10, 9, 12, 30, 45, 123, DateTimeKind.Utc);
        var user = User.Create(UserType.Teacher, precisTime);

        // Act
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Clear and reload
        _context.ChangeTracker.Clear();
        var savedUser = await _context.Users.FindAsync(user.Id);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.CreatedAtUtc.Should().BeCloseTo(precisTime, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Database_ShouldEnforceUniqueConstraint_OnUserRolePerSchool()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        var role = Role.Create("Teacher", "Teacher role", _testTime);
        var school = School.Create("test-school", "Test School", "EMIS001", "Test Location", _testTime);

        await _context.Users.AddAsync(user);
        await _context.Roles.AddAsync(role);
        await _context.Schools.AddAsync(school);
        await _context.SaveChangesAsync();

        var membership1 = UserSchoolMembership.Create(user.Id, school.Id, role.Id, _testTime);
        await _context.UserSchoolMemberships.AddAsync(membership1);
        await _context.SaveChangesAsync();

        // Act - Try to add duplicate membership
        var membership2 = UserSchoolMembership.Create(user.Id, school.Id, role.Id, _testTime.AddMinutes(1));
        await _context.UserSchoolMemberships.AddAsync(membership2);

        Func<Task> act = async () => await _context.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Database_ShouldQueryUsers_ByTypeAndStatus_Efficiently()
    {
        // Arrange
        var admin = User.Create(UserType.SystemAdmin, _testTime);
        admin.Activate(_testTime);

        var teacher1 = User.Create(UserType.Teacher, _testTime);
        teacher1.Activate(_testTime);

        var teacher2 = User.Create(UserType.Teacher, _testTime);
        // teacher2 remains pending

        await _context.Users.AddRangeAsync(admin, teacher1, teacher2);
        await _context.SaveChangesAsync();

        // Act
        var activeTeachers = await _context.Users
            .Where(u => u.Type == UserType.Teacher && u.Status == UserStatus.Active)
            .ToListAsync();

        // Assert
        activeTeachers.Should().HaveCount(1);
        activeTeachers[0].Id.Should().Be(teacher1.Id);
    }

    [Fact]
    public async Task Database_ShouldSupportComplexQuery_ForUserPermissions()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        var role = Role.Create("Teacher", "Teacher role", _testTime);
        var permission = Permission.Create("students.view", "View students", _testTime);

        await _context.Users.AddAsync(user);
        await _context.Roles.AddAsync(role);
        await _context.Permissions.AddAsync(permission);
        await _context.SaveChangesAsync();

        var rolePermission = RolePermission.Create(role.Id, permission.Id, _testTime);
        await _context.RolePermissions.AddAsync(rolePermission);

        var membership = UserSchoolMembership.Create(user.Id, null, role.Id, _testTime);
        membership.Activate(_testTime);
        await _context.UserSchoolMemberships.AddAsync(membership);
        await _context.SaveChangesAsync();

        // Act - Query user permissions through joins
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
            .ToListAsync();

        // Assert
        permissions.Should().HaveCount(1);
        permissions[0].Should().Be("students.view");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

