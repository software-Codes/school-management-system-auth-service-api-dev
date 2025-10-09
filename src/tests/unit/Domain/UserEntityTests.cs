using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Events;
using AuthService.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace AuthService.UnitTests.Domain;

public class UserEntityTests
{
    private readonly DateTime _testTime = new DateTime(2025, 10, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidUserType_ShouldCreateUserSuccessfully()
    {
        // Act
        var user = User.Create(UserType.SystemAdmin, _testTime);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.Type.Should().Be(UserType.SystemAdmin);
        user.Status.Should().Be(UserStatus.Pending);
        user.CreatedAtUtc.Should().Be(_testTime);
        user.UpdatedAtUtc.Should().Be(_testTime);
    }

    [Fact]
    public void Create_ShouldRaiseUserCreatedEvent()
    {
        // Act
        var user = User.Create(UserType.Principal, _testTime);

        // Assert
        user.DomainEvents.Should().HaveCount(1);
        var domainEvent = user.DomainEvents.First();
        domainEvent.Should().BeOfType<UserCreatedEvent>();

        var userCreatedEvent = (UserCreatedEvent)domainEvent;
        userCreatedEvent.UserId.Should().Be(user.Id);
        userCreatedEvent.UserType.Should().Be(UserType.Principal);
        userCreatedEvent.OccurredAtUtc.Should().Be(_testTime);
    }

    [Fact]
    public void Create_WithInvalidUserType_ShouldThrowDomainException()
    {
        // Arrange
        var invalidUserType = (UserType)999;

        // Act
        Action act = () => User.Create(invalidUserType, _testTime);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage($"Invalid user type: {invalidUserType}");
    }

    [Fact]
    public void Activate_WhenPending_ShouldActivateUserAndRaiseEvent()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        user.ClearDomainEvents();
        var activationTime = _testTime.AddMinutes(5);

        // Act
        user.Activate(activationTime);

        // Assert
        user.Status.Should().Be(UserStatus.Active);
        user.UpdatedAtUtc.Should().Be(activationTime);
        user.DomainEvents.Should().HaveCount(1);

        var statusChangedEvent = user.DomainEvents.First() as UserStatusChangedEvent;
        statusChangedEvent.Should().NotBeNull();
        statusChangedEvent!.UserId.Should().Be(user.Id);
        statusChangedEvent.OldStatus.Should().Be(UserStatus.Pending);
        statusChangedEvent.NewStatus.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldBeIdempotent()
    {
        // Arrange
        var user = User.Create(UserType.Parent, _testTime);
        user.Activate(_testTime);
        user.ClearDomainEvents();

        // Act
        user.Activate(_testTime.AddMinutes(10));

        // Assert
        user.Status.Should().Be(UserStatus.Active);
        user.DomainEvents.Should().BeEmpty("activation is idempotent");
    }

    [Fact]
    public void IsActive_WhenStatusIsActive_ShouldReturnTrue()
    {
        // Arrange
        var user = User.Create(UserType.Student, _testTime);
        user.Activate(_testTime);

        // Act
        var isActive = user.IsActive();

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenStatusIsNotActive_ShouldReturnFalse()
    {
        // Arrange
        var user = User.Create(UserType.Student, _testTime);

        // Act
        var isActive = user.IsActive();

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void Disable_WhenActive_ShouldDisableUser()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        user.Activate(_testTime);
        user.ClearDomainEvents();
        var disableTime = _testTime.AddDays(1);

        // Act
        user.Disable(disableTime, "Violation of terms");

        // Assert
        user.Status.Should().Be(UserStatus.Disabled);
        user.UpdatedAtUtc.Should().Be(disableTime);
        user.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Disable_SystemAdmin_ShouldThrowException()
    {
        // Arrange
        var user = User.Create(UserType.SystemAdmin, _testTime);
        user.Activate(_testTime);

        // Act
        Action act = () => user.Disable(_testTime.AddDays(1));

        // Assert
        act.Should().Throw<InvalidUserStateException>()
            .WithMessage("System administrators cannot be disabled.");
    }

    [Fact]
    public void Disable_WhenAlreadyDisabled_ShouldBeIdempotent()
    {
        // Arrange
        var user = User.Create(UserType.Parent, _testTime);
        user.Activate(_testTime);
        user.Disable(_testTime.AddDays(1));
        user.ClearDomainEvents();

        // Act
        user.Disable(_testTime.AddDays(2));

        // Assert
        user.Status.Should().Be(UserStatus.Disabled);
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Lock_WhenActive_ShouldLockUser()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        user.Activate(_testTime);
        user.ClearDomainEvents();
        var lockTime = _testTime.AddHours(1);

        // Act
        user.Lock(lockTime);

        // Assert
        user.Status.Should().Be(UserStatus.Locked);
        user.IsLocked().Should().BeTrue();
        user.UpdatedAtUtc.Should().Be(lockTime);
    }

    [Fact]
    public void Unlock_WhenLocked_ShouldActivateUser()
    {
        // Arrange
        var user = User.Create(UserType.Parent, _testTime);
        user.Activate(_testTime);
        user.Lock(_testTime.AddHours(1));
        user.ClearDomainEvents();
        var unlockTime = _testTime.AddHours(2);

        // Act
        user.Unlock(unlockTime);

        // Assert
        user.Status.Should().Be(UserStatus.Active);
        user.IsActive().Should().BeTrue();
        user.UpdatedAtUtc.Should().Be(unlockTime);
        user.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Unlock_WhenNotLocked_ShouldThrowException()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        user.Activate(_testTime);

        // Act
        Action act = () => user.Unlock(_testTime.AddHours(1));

        // Assert
        act.Should().Throw<InvalidUserStateException>()
            .WithMessage("Only locked users can be unlocked.");
    }

    [Fact]
    public void AddMembership_WithValidMembership_ShouldAddSuccessfully()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        var schoolId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var membership = UserSchoolMembership.Create(user.Id, schoolId, roleId, _testTime);
        membership.Activate(_testTime); // Must activate for HasMembershipInSchool to return true

        // Act
        user.AddMembership(membership, _testTime);

        // Assert
        user.Memberships.Should().HaveCount(1);
        user.Memberships.First().Should().Be(membership);
        user.HasMembershipInSchool(schoolId).Should().BeTrue();
    }

    [Fact]
    public void AddMembership_WithDuplicateRole_ShouldThrowException()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        var schoolId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var membership1 = UserSchoolMembership.Create(user.Id, schoolId, roleId, _testTime);
        var membership2 = UserSchoolMembership.Create(user.Id, schoolId, roleId, _testTime.AddMinutes(1));
        user.AddMembership(membership1, _testTime);

        // Act
        Action act = () => user.AddMembership(membership2, _testTime.AddMinutes(1));

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("User already has this role in this school.");
    }

    [Fact]
    public void RemoveMembership_WhenExists_ShouldRemoveSuccessfully()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        var schoolId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var membership = UserSchoolMembership.Create(user.Id, schoolId, roleId, _testTime);
        user.AddMembership(membership, _testTime);

        // Act
        user.RemoveMembership(membership.Id, _testTime.AddDays(1));

        // Assert
        user.Memberships.Should().BeEmpty();
        user.HasMembershipInSchool(schoolId).Should().BeFalse();
    }

    [Fact]
    public void HasMembershipInSchool_WhenMembershipExists_ShouldReturnTrue()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        var schoolId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var membership = UserSchoolMembership.Create(user.Id, schoolId, roleId, _testTime);
        membership.Activate(_testTime);
        user.AddMembership(membership, _testTime);

        // Act
        var hasMembership = user.HasMembershipInSchool(schoolId);

        // Assert
        hasMembership.Should().BeTrue();
    }

    [Fact]
    public void HasMembershipInSchool_WhenNoMembership_ShouldReturnFalse()
    {
        // Arrange
        var user = User.Create(UserType.Teacher, _testTime);
        var schoolId = Guid.NewGuid();

        // Act
        var hasMembership = user.HasMembershipInSchool(schoolId);

        // Assert
        hasMembership.Should().BeFalse();
    }
}

