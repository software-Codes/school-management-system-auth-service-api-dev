using AuthService.Domain.Common;
using AuthService.Domain.Enums;
using AuthService.Domain.Events;
using AuthService.Domain.Exceptions;

namespace AuthService.Domain.Entities;


public class User : BaseEntity
{
    // EF Core requires parameterless constructor
    private User() { }

    public static User Create(UserType type, DateTime utcNow)
    {
        if (!Enum.IsDefined(typeof(UserType), type))
            throw new DomainException($"Invalid user type: {type}");

        var user = new User
        {
            Id = Guid.NewGuid(),  
            Type = type,
            Status = UserStatus.Pending, 
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, user.Type, utcNow));

        return user;
    }

    public UserType Type { get; private set; }
    public UserStatus Status { get; private set; }

    private readonly List<UserSchoolMembership> _memberships = new();
    public IReadOnlyCollection<UserSchoolMembership> Memberships => _memberships.AsReadOnly();


    public void Activate(DateTime utcNow)
    {
        if (Status == UserStatus.Active)
            return; // Idempotent

        var oldStatus = Status;
        Status = UserStatus.Active;
        MarkAsUpdated(utcNow);

        RaiseDomainEvent(new UserStatusChangedEvent(Id, oldStatus, Status, utcNow));
    }

    public void Disable(DateTime utcNow, string? reason = null)
    {
        if (Status == UserStatus.Disabled)
            return;

        if (Type == UserType.SystemAdmin)
            throw new InvalidUserStateException("System administrators cannot be disabled.");

        var oldStatus = Status;
        Status = UserStatus.Disabled;
        MarkAsUpdated(utcNow);

        RaiseDomainEvent(new UserStatusChangedEvent(Id, oldStatus, Status, utcNow));
    }

    public void Lock(DateTime utcNow)
    {
        if (Status == UserStatus.Locked)
            return;

        var oldStatus = Status;
        Status = UserStatus.Locked;
        MarkAsUpdated(utcNow);

        RaiseDomainEvent(new UserStatusChangedEvent(Id, oldStatus, Status, utcNow));
    }

    public void Unlock(DateTime utcNow)
    {
        if (Status != UserStatus.Locked)
            throw new InvalidUserStateException("Only locked users can be unlocked.");

        var oldStatus = Status;
        Status = UserStatus.Active;
        MarkAsUpdated(utcNow);

        RaiseDomainEvent(new UserStatusChangedEvent(Id, oldStatus, Status, utcNow));
    }

    public void AddMembership(UserSchoolMembership membership, DateTime utcNow)
    {
        if (membership == null)
            throw new ArgumentNullException(nameof(membership));

        if (_memberships.Any(m => m.SchoolId == membership.SchoolId && m.RoleId == membership.RoleId))
            throw new DomainException("User already has this role in this school.");

        _memberships.Add(membership);
        MarkAsUpdated(utcNow);
    }

    public void RemoveMembership(Guid membershipId, DateTime utcNow)
    {
        var membership = _memberships.FirstOrDefault(m => m.Id == membershipId);
        if (membership != null)
        {
            _memberships.Remove(membership);
            MarkAsUpdated(utcNow);
        }
    }

    public bool IsActive() => Status == UserStatus.Active;
    public bool IsLocked() => Status == UserStatus.Locked;
    public bool HasMembershipInSchool(Guid schoolId) => 
        _memberships.Any(m => m.SchoolId == schoolId && m.Status == MembershipStatus.Active);
}
