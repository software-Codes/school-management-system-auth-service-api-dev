using AuthService.Abstractions.Common;
using AuthService.Abstractions.Security;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.Infrastructure.Persistence.Seeding.Seeders;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthService.UnitTests.Seeders;

public class SystemAdminSeederTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<SystemAdminSeeder>> _loggerMock;
    private readonly SystemAdminSeeder _seeder;
    private readonly DateTime _testTime = new DateTime(2025, 10, 9, 12, 0, 0, DateTimeKind.Utc);

    public SystemAdminSeederTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new IdentityDbContext(options);

        // Setup mocks
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(_testTime);

        _passwordHasherMock = new Mock<IPasswordHasher>();
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns(new byte[48]); // Mock hash

        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(x => x["SeedData:SystemAdmin:Email"])
            .Returns("admin@platform.local");
        _configurationMock.Setup(x => x["SeedData:SystemAdmin:TempPassword"])
            .Returns("ChangeMe123!");

        _loggerMock = new Mock<ILogger<SystemAdminSeeder>>();

        _seeder = new SystemAdminSeeder(
            _context,
            _dateTimeProviderMock.Object,
            _passwordHasherMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SeedAsync_WhenSystemAdminDoesNotExist_ShouldCreateSystemAdmin()
    {
        // Arrange - Create SystemAdmin role first
        var systemAdminRole = Role.Create("SystemAdmin", "System Administrator with full access", _testTime);
        await _context.Roles.AddAsync(systemAdminRole);
        await _context.SaveChangesAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Type == UserType.SystemAdmin);
        user.Should().NotBeNull();
        user!.Type.Should().Be(UserType.SystemAdmin);
        user.Status.Should().Be(UserStatus.Active);
        user.CreatedAtUtc.Should().Be(_testTime);
    }

    [Fact]
    public async Task SeedAsync_WhenSystemAdminDoesNotExist_ShouldCreateContact()
    {
        // Arrange
        var systemAdminRole = Role.Create("SystemAdmin", "System Administrator", _testTime);
        await _context.Roles.AddAsync(systemAdminRole);
        await _context.SaveChangesAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Value == "admin@platform.local");
        contact.Should().NotBeNull();
        contact!.Kind.Should().Be(ContactKind.Email);
        contact.IsPrimary.Should().BeTrue();
        contact.IsVerified.Should().BeTrue();
        contact.VerifiedAt.Should().Be(_testTime);
    }

    [Fact]
    public async Task SeedAsync_WhenSystemAdminDoesNotExist_ShouldCreateCredential()
    {
        // Arrange
        var systemAdminRole = Role.Create("SystemAdmin", "System Administrator", _testTime);
        await _context.Roles.AddAsync(systemAdminRole);
        await _context.SaveChangesAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Type == UserType.SystemAdmin);
        var credential = await _context.Credentials.FirstOrDefaultAsync(c => c.UserId == user!.Id);
        
        credential.Should().NotBeNull();
        credential!.MfaMode.Should().Be(MfaMode.PasswordAndOtp);
        credential.MustChangePassword.Should().BeTrue();
        credential.PasswordHash.Should().NotBeNull();
        
        _passwordHasherMock.Verify(x => x.HashPassword("ChangeMe123!"), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenSystemAdminDoesNotExist_ShouldCreateMembership()
    {
        // Arrange
        var systemAdminRole = Role.Create("SystemAdmin", "System Administrator", _testTime);
        await _context.Roles.AddAsync(systemAdminRole);
        await _context.SaveChangesAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Type == UserType.SystemAdmin);
        var membership = await _context.UserSchoolMemberships
            .FirstOrDefaultAsync(m => m.UserId == user!.Id);
        
        membership.Should().NotBeNull();
        membership!.RoleId.Should().Be(systemAdminRole.Id);
        membership.SchoolId.Should().BeNull("SystemAdmin is not tied to a specific school");
        membership.Status.Should().Be(MembershipStatus.Active);
    }

    [Fact]
    public async Task SeedAsync_WhenSystemAdminAlreadyExists_ShouldNotCreateDuplicate()
    {
        // Arrange
        var systemAdminRole = Role.Create("SystemAdmin", "System Administrator", _testTime);
        await _context.Roles.AddAsync(systemAdminRole);
        
        var existingUser = User.Create(UserType.SystemAdmin, _testTime);
        await _context.Users.AddAsync(existingUser);
        await _context.SaveChangesAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        var userCount = await _context.Users.CountAsync(u => u.Type == UserType.SystemAdmin);
        userCount.Should().Be(1, "should not create duplicate system admin");
    }

    [Fact]
    public async Task SeedAsync_WhenSystemAdminRoleDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Act
        Func<Task> act = async () => await _seeder.SeedAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SystemAdmin role not found. Cannot create system admin user.");
    }

    [Fact]
    public async Task SeedAsync_ShouldUseConfiguredEmail()
    {
        // Arrange
        _configurationMock.Setup(x => x["SeedData:SystemAdmin:Email"])
            .Returns("custom@admin.com");
        
        var systemAdminRole = Role.Create("SystemAdmin", "System Administrator", _testTime);
        await _context.Roles.AddAsync(systemAdminRole);
        await _context.SaveChangesAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        var contact = await _context.Contacts.FirstOrDefaultAsync();
        contact.Should().NotBeNull();
        contact!.Value.Should().Be("custom@admin.com");
    }

    [Fact]
    public async Task SeedAsync_WhenEmailNotConfigured_ShouldUseDefaultEmail()
    {
        // Arrange
        _configurationMock.Setup(x => x["SeedData:SystemAdmin:Email"])
            .Returns((string?)null);
        
        var systemAdminRole = Role.Create("SystemAdmin", "System Administrator", _testTime);
        await _context.Roles.AddAsync(systemAdminRole);
        await _context.SaveChangesAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        var contact = await _context.Contacts.FirstOrDefaultAsync();
        contact.Should().NotBeNull();
        contact!.Value.Should().Be("admin@platform.local");
    }

    [Fact]
    public async Task SeedAsync_WhenPasswordNotConfigured_ShouldUseDefaultPassword()
    {
        // Arrange
        _configurationMock.Setup(x => x["SeedData:SystemAdmin:TempPassword"])
            .Returns((string?)null);
        
        var systemAdminRole = Role.Create("SystemAdmin", "System Administrator", _testTime);
        await _context.Roles.AddAsync(systemAdminRole);
        await _context.SaveChangesAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        _passwordHasherMock.Verify(x => x.HashPassword("ChangeMe123!"), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_ShouldPersistAllChangesToDatabase()
    {
        // Arrange
        var systemAdminRole = Role.Create("SystemAdmin", "System Administrator", _testTime);
        await _context.Roles.AddAsync(systemAdminRole);
        await _context.SaveChangesAsync();

        // Act
        await _seeder.SeedAsync();

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Type == UserType.SystemAdmin);
        var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.UserId == user!.Id);
        var credential = await _context.Credentials.FirstOrDefaultAsync(c => c.UserId == user!.Id);
        var membership = await _context.UserSchoolMemberships.FirstOrDefaultAsync(m => m.UserId == user!.Id);

        user.Should().NotBeNull("user should be persisted");
        contact.Should().NotBeNull("contact should be persisted");
        credential.Should().NotBeNull("credential should be persisted");
        membership.Should().NotBeNull("membership should be persisted");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

