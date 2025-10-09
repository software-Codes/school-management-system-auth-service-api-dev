using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace AuthService.UnitTests.Domain;

public class SchoolEntityTests
{
    private readonly DateTime _testTime = new DateTime(2025, 10, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidData_ShouldCreateSchool()
    {
        // Act
        var school = School.Create(
            slug: "test-school",
            officialName: "Test School",
            emisCode: "EMIS001",
            location: "Test Location",
            utcNow: _testTime,
            email: "test@school.com",
            phone: "+1234567890",
            address: "123 Test Street"
        );

        // Assert
        school.Should().NotBeNull();
        school.Id.Should().NotBeEmpty();
        school.Slug.Should().Be("test-school");
        school.OfficialName.Should().Be("Test School");
        school.EmisCode.Should().Be("EMIS001");
        school.Location.Should().Be("Test Location");
        school.Email.Should().Be("test@school.com");
        school.Phone.Should().Be("+1234567890");
        school.Address.Should().Be("123 Test Street");
        school.Status.Should().Be(SchoolStatus.Active);
        school.CreatedAtUtc.Should().Be(_testTime);
        school.UpdatedAtUtc.Should().Be(_testTime);
    }

    [Fact]
    public void Create_WithMinimalData_ShouldCreateSchool()
    {
        // Act
        var school = School.Create(
            slug: "minimal-school",
            officialName: "Minimal School",
            emisCode: "MIN001",
            location: "Minimal Location",
            utcNow: _testTime
        );

        // Assert
        school.Should().NotBeNull();
        school.Slug.Should().Be("minimal-school");
        school.OfficialName.Should().Be("Minimal School");
        school.EmisCode.Should().Be("MIN001");
        school.Location.Should().Be("Minimal Location");
        school.Email.Should().BeNull();
        school.Phone.Should().BeNull();
        school.Address.Should().BeNull();
        school.Status.Should().Be(SchoolStatus.Active);
    }

    [Fact]
    public void Create_ShouldNormalizeSlugToLowercase()
    {
        // Act
        var school = School.Create(
            slug: "UPPERCASE-SCHOOL",
            officialName: "Test School",
            emisCode: "TEST001",
            location: "Test Location",
            utcNow: _testTime
        );

        // Assert
        school.Slug.Should().Be("uppercase-school");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Act
        var school = School.Create(
            slug: "  test-school  ",
            officialName: "  Test School  ",
            emisCode: "  EMIS001  ",
            location: "Test Location",
            utcNow: _testTime
        );

        // Assert
        school.Slug.Should().Be("test-school");
        school.OfficialName.Should().Be("Test School");
        school.EmisCode.Should().Be("EMIS001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidSlug_ShouldThrowArgumentException(string? invalidSlug)
    {
        // Act
        Action act = () => School.Create(
            slug: invalidSlug!,
            officialName: "Test School",
            emisCode: "TEST001",
            location: "Test Location",
            utcNow: _testTime
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("School slug cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithInvalidOfficialName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act
        Action act = () => School.Create(
            slug: "test-school",
            officialName: invalidName!,
            emisCode: "TEST001",
            location: "Test Location",
            utcNow: _testTime
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("School name cannot be empty*");
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateSchool()
    {
        // Arrange
        var school = School.Create("test-school", "Original Name", "ORIG001", "Original Location", _testTime);
        var updateTime = _testTime.AddDays(1);

        // Act
        school.UpdateDetails(
            officialName: "Updated Name",
            location: "Updated Location",
            emisCode: "UPD001",
            email: "updated@school.com",
            phone: "+9876543210",
            address: "456 Updated Street",
            utcNow: updateTime
        );

        // Assert
        school.OfficialName.Should().Be("Updated Name");
        school.Location.Should().Be("Updated Location");
        school.EmisCode.Should().Be("UPD001");
        school.Email.Should().Be("updated@school.com");
        school.Phone.Should().Be("+9876543210");
        school.Address.Should().Be("456 Updated Street");
        school.UpdatedAtUtc.Should().Be(updateTime);
    }

    [Fact]
    public void Activate_ShouldSetStatusToActive()
    {
        // Arrange
        var school = School.Create("test-school", "Test School", "TEST001", "Test Location", _testTime);
        school.Suspend(_testTime);
        var activateTime = _testTime.AddDays(1);

        // Act
        school.Activate(activateTime);

        // Assert
        school.Status.Should().Be(SchoolStatus.Active);
        school.UpdatedAtUtc.Should().Be(activateTime);
    }

    [Fact]
    public void Suspend_ShouldSetStatusToSuspended()
    {
        // Arrange
        var school = School.Create("test-school", "Test School", "TEST001", "Test Location", _testTime);
        var suspendTime = _testTime.AddDays(1);

        // Act
        school.Suspend(suspendTime);

        // Assert
        school.Status.Should().Be(SchoolStatus.Suspended);
        school.UpdatedAtUtc.Should().Be(suspendTime);
    }

    [Fact]
    public void Close_ShouldSetStatusToClosed()
    {
        // Arrange
        var school = School.Create("test-school", "Test School", "TEST001", "Test Location", _testTime);
        var closeTime = _testTime.AddDays(1);

        // Act
        school.Close(closeTime);

        // Assert
        school.Status.Should().Be(SchoolStatus.Closed);
        school.UpdatedAtUtc.Should().Be(closeTime);
    }

    [Fact]
    public void UpdateContactInfo_ShouldUpdateEmailAndPhone()
    {
        // Arrange
        var school = School.Create("test-school", "Test School", "TEST001", "Test Location", _testTime);
        var updateTime = _testTime.AddDays(1);

        // Act
        school.UpdateContactInfo("new@school.com", "+1111111111", updateTime);

        // Assert
        school.Email.Should().Be("new@school.com");
        school.Phone.Should().Be("+1111111111");
        school.UpdatedAtUtc.Should().Be(updateTime);
    }
}