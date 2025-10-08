using AuthService.Domain.Common;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Events;

public sealed class UserStatusChangedEvent : IDomainEvent
{
    public UserStatusChangedEvent(Guid userId, UserStatus oldStatus, UserStatus newStatus, DateTime occurredAtUtc)
    {
        EventId = Guid.NewGuid();
        UserId = userId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid EventId { get; }
    public Guid UserId { get; }
    public UserStatus OldStatus { get; }
    public UserStatus NewStatus { get; }
    public DateTime OccurredAtUtc { get; }
}
