using AuthService.Domain.Common;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Events;

public sealed class UserCreatedEvent : IDomainEvent
{
    public UserCreatedEvent(Guid userId, UserType userType, DateTime occurredAtUtc)
    {
        EventId = Guid.NewGuid();
        UserId = userId;
        UserType = userType;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid EventId { get; }
    public Guid UserId { get; }
    public UserType UserType { get; }
    public DateTime OccurredAtUtc { get; }
}
