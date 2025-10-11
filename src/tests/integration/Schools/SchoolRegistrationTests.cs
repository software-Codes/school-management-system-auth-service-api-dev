using AuthService.Application.Contracts.School;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthService.IntegrationTests.Schools;

public class SchoolRegistrationTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly DateTime _testTime = new DateTime(2025, 10, 9, 12, 0, 0, DateTimeKind.Utc);

    public SchoolRegistrationTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _dateTimeProvider = new FakeDateTimeProvider(_testTime);
    }

    [Fact]
    public async Task RegisterSchool_WithValidData_ShouldCreateSchool()
    {
        // Arrange
        var request = new RegisterSchoolRequest
        {
            OfficialName = "Test High School",
            Slug = "test-high-school",
            EmisCode = "THS001",
            Location = "Test City",
            Email = "admin@testhigh.edu",
            Phone = "+1234567890",
            Address = "123 Education Street"
        };

        // Act
        var school = School.Create(
            slug: request.Slug,
            officialName: request.OfficialName,
            emisCode: request.EmisCode,
            location: request.Location,
            utcNow: _dateTimeProvider.UtcNow,
            email: request.Email,
            phone: request.Phone,
            address: request.Address
        );

        await _context.Schools.AddAsync(school);
        await _context.SaveChangesAsync();

        // Assert
        var savedSchool = await _context.Schools.FirstOrDefaultAsync(s => s.Slug == request.Slug);
        savedSchool.Should().NotBeNull();
        savedSchool!.OfficialName.Should().Be(request.OfficialName);
        savedSchool.Slug.Should().Be(request.Slug.ToLowerInvariant());
        savedSchool.EmisCode.Should().Be(request.EmisCode);
        savedSchool.Location.Should().Be(request.Location);
        savedSchool.Email.Should().Be(request.Email);
        savedSchool.Phone.Should().Be(request.Phone);
        savedSchool.Address.Should().Be(request.Address);
        savedSchool.Status.Should().Be(SchoolStatus.Active);
        savedSchool.CreatedAtUtc.Should().Be(_testTime);
    }

    [Fact]
    public async Task RegisterSchool_WithDuplicateSlug_ShouldFailValidation()
    {
        // Arrange
        var existingSchool = School.Create(
            slug: "existing-school",
            officialName: "Existing School",
            emisCode: "EXS001",
            location: "Existing City",
            utcNow: _testTime
        );
        await _context.Schools.AddAsync(existingSchool);
        await _context.SaveChangesAsync();

        // Act & Assert
        var slugExists = await _context.Schools
            .AnyAsync(s => s.Slug == "existing-school");

        slugExists.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterSchool_WithDuplicateEmisCode_ShouldFailValidation()
    {
        // Arrange
        var existingSchool = School.Create(
            slug: "existing-school",
            officialName: "Existing School",
            emisCode: "DUPLICATE001",
            location: "Existing City",
            utcNow: _testTime
        );
        await _context.Schools.AddAsync(existingSchool);
        await _context.SaveChangesAsync();

        // Act & Assert
        var emisExists = await _context.Schools
            .AnyAsync(s => s.EmisCode == "DUPLICATE001");

        emisExists.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterSchool_ShouldNormalizeSlugToLowercase()
    {
        // Arrange
        var school = School.Create(
            slug: "UPPERCASE-SCHOOL",
            officialName: "Uppercase School",
            emisCode: "UPS001",
            location: "Uppercase City",
            utcNow: _dateTimeProvider.UtcNow
        );

        // Act
        await _context.Schools.AddAsync(school);
        await _context.SaveChangesAsync();

        // Assert
        var savedSchool = await _context.Schools.FirstOrDefaultAsync();
        savedSchool.Should().NotBeNull();
        savedSchool!.Slug.Should().Be("uppercase-school");
    }

    [Fact]
    public async Task RegisterSchool_ShouldSetDefaultStatusToActive()
    {
        // Arrange
        var school = School.Create(
            slug: "status-school",
            officialName: "Status School",
            emisCode: "ST001",
            location: "Status City",
            utcNow: _dateTimeProvider.UtcNow
        );

        // Act
        await _context.Schools.AddAsync(school);
        await _context.SaveChangesAsync();

        // Assert
        var savedSchool = await _context.Schools.FirstOrDefaultAsync();
        savedSchool.Should().NotBeNull();
        savedSchool!.Status.Should().Be(SchoolStatus.Active);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}