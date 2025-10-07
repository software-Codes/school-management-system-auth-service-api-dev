using AuthService.Abstractions.Common;

namespace AuthService.Infrastructure.Time;

/// <summary>
/// Production implementation of time provider
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>
/// Test/Fake implementation - allows controlling time in tests
/// </summary>
public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    private DateTime _currentTime = DateTime.UtcNow;

    public DateTime UtcNow => _currentTime;

    public void SetTime(DateTime time) => _currentTime = time;
    public void AdvanceBy(TimeSpan duration) => _currentTime = _currentTime.Add(duration);
}
