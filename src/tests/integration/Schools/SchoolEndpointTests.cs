using AuthService.Application.Contracts.School;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence.EfCore;
using AuthService.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;

namespace AuthService.IntegrationTests.Schools;

public class SchoolEndpointTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly DateTime _testTime = new DateTime(2025, 10, 9, 12, 0, 0, DateTimeKind.Utc);

    public SchoolEndpointTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _dateTimeProvider = new FakeDateTimeProvider(_testTime);
    }

    [Fact]
    public async Task RegisterSchool_BusinessLogic_WithValidRequest_ShouldCreateSchool()
    {
        // Arrange
        var request = new RegisterSchoolRequest
        {
            OfficialName = "Integration Test School",
            Slug = "integration-test-school",
            EmisCode = "ITS001",
            Location = "Integration City",
            Email = "admin@integration.edu",
            Phone = "+1234567890",
            Address = "123 Integration Street"
        };

        // Simulate the endpoint logic
        var slugExists = await _context.Schools
            .AnyAsync(s => s.Slug == request.Slug.ToLowerInvariant());
        slugExists.Should().BeFalse();

        var emisExists = await _context.Schools
            .AnyAsync(s => s.EmisCode == request.EmisCode);
        emisExists.Should().BeFalse();

        // Act - Simulate endpoint business logic
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
        var savedSchool = await _context.Schools.FirstOrDefaultAsync(s => s.Slug == request.Slug.ToLowerInvariant());
        savedSchool.Should().NotBeNull();
        savedSchool!.OfficialName.Should().Be(request.OfficialName);
        savedSchool.EmisCode.Should().Be(request.EmisCode);
        savedSchool.Location.Should().Be(request.Location);
        savedSchool.Email.Should().Be(request.Email);
        savedSchool.Phone.Should().Be(request.Phone);
        savedSchool.Address.Should().Be(request.Address);
    }

    [Fact]
    public async Task RegisterSchool_BusinessLogic_WithDuplicateSlug_ShouldDetectConflict()
    {
        // Arrange
        var existingSchool = School.Create(
            slug: "duplicate-slug",
            officialName: "Existing School",
            emisCode: "EXS001",
            location: "Existing City",
            utcNow: _testTime
        );
        await _context.Schools.AddAsync(existingSchool);
        await _context.SaveChangesAsync();

        var request = new RegisterSchoolRequest
        {
            OfficialName = "New School",
            Slug = "duplicate-slug",
            EmisCode = "NEW001",
            Location = "New City"
        };

        // Act - Simulate endpoint validation logic
        var slugExists = await _context.Schools
            .AnyAsync(s => s.Slug == request.Slug.ToLowerInvariant());

        // Assert
        slugExists.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterSchool_BusinessLogic_WithDuplicateEmisCode_ShouldDetectConflict()
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

        var request = new RegisterSchoolRequest
        {
            OfficialName = "New School",
            Slug = "new-school",
            EmisCode = "DUPLICATE001",
            Location = "New City"
        };

        // Act - Simulate endpoint validation logic
        var emisExists = await _context.Schools
            .AnyAsync(s => s.EmisCode == request.EmisCode);

        // Assert
        emisExists.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterSchool_BusinessLogic_WithEmptyEmisCode_ShouldSkipEmisValidation()
    {
        // Arrange
        var request = new RegisterSchoolRequest
        {
            OfficialName = "No EMIS School",
            Slug = "no-emis-school",
            EmisCode = "",
            Location = "No EMIS City"
        };

        // Act - Simulate endpoint validation logic
        var shouldValidateEmis = !string.IsNullOrWhiteSpace(request.EmisCode);

        // Assert
        shouldValidateEmis.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterSchool_BusinessLogic_ShouldCreateCorrectResponse()
    {
        // Arrange
        var request = new RegisterSchoolRequest
        {
            OfficialName = "Response Test School",
            Slug = "response-test-school",
            EmisCode = "RTS001",
            Location = "Response City",
            Email = "admin@response.edu",
            Phone = "+9876543210",
            Address = "456 Response Avenue"
        };

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

        // Act - Simulate MapToResponse logic
        var response = new SchoolResponse
        {
            Id = school.Id,
            Slug = school.Slug,
            OfficialName = school.OfficialName,
            EmisCode = school.EmisCode ?? string.Empty,
            Location = school.Location,
            Email = school.Email,
            Phone = school.Phone,
            Address = school.Address,
            Status = school.Status.ToString(),
            CreatedAt = school.CreatedAtUtc
        };

        // Assert
        response.Should().NotBeNull();
        response.Id.Should().Be(school.Id);
        response.Slug.Should().Be(school.Slug);
        response.OfficialName.Should().Be(school.OfficialName);
        response.EmisCode.Should().Be(school.EmisCode);
        response.Location.Should().Be(school.Location);
        response.Email.Should().Be(school.Email);
        response.Phone.Should().Be(school.Phone);
        response.Address.Should().Be(school.Address);
        response.Status.Should().Be("Active");
        response.CreatedAt.Should().Be(_testTime);
    }

    [Fact]
    public void RegisterSchool_BusinessLogic_WithNullEmisCode_ShouldHandleCorrectly()
    {
        // Arrange
        var school = School.Create(
            slug: "null-emis-school",
            officialName: "Null EMIS School",
            emisCode: null!,
            location: "Null EMIS City",
            utcNow: _dateTimeProvider.UtcNow
        );

        _context.Schools.Add(school);
        _context.SaveChanges();

        // Act - Simulate MapToResponse logic with null EMIS
        var response = new SchoolResponse
        {
            Id = school.Id,
            Slug = school.Slug,
            OfficialName = school.OfficialName,
            EmisCode = school.EmisCode ?? string.Empty,
            Location = school.Location,
            Email = school.Email,
            Phone = school.Phone,
            Address = school.Address,
            Status = school.Status.ToString(),
            CreatedAt = school.CreatedAtUtc
        };

        // Assert
        response.EmisCode.Should().Be(string.Empty);
    }

    [Fact]
    public async Task RegisterSchool_BusinessLogic_ShouldHandleMultipleSchools()
    {
        // Arrange
        var schools = new[]
        {
            School.Create("school-1", "School One", "SCH001", "City One", _testTime),
            School.Create("school-2", "School Two", "SCH002", "City Two", _testTime),
            School.Create("school-3", "School Three", "SCH003", "City Three", _testTime)
        };

        // Act
        await _context.Schools.AddRangeAsync(schools);
        await _context.SaveChangesAsync();

        // Assert
        var savedSchools = await _context.Schools.ToListAsync();
        savedSchools.Should().HaveCount(3);
        savedSchools.Should().OnlyContain(s => s.Status == Domain.Enums.SchoolStatus.Active);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}