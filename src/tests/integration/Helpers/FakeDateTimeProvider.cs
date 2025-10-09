using AuthService.Abstractions.Common;

namespace AuthService.IntegrationTests.Helpers;

/// <summary>
/// Fake implementation of IDateTimeProvider for testing
/// Allows controlling time in tests
/// </summary>
public class FakeDateTimeProvider : IDateTimeProvider
{
    private DateTime _currentTime;

    public FakeDateTimeProvider(DateTime? initialTime = null)
    {
        _currentTime = initialTime ?? new DateTime(2025, 10, 9, 12, 0, 0, DateTimeKind.Utc);
    }

    public DateTime UtcNow => _currentTime;

    public void SetTime(DateTime time)
    {
        _currentTime = time;
    }

    public void AdvanceTime(TimeSpan timeSpan)
    {
        _currentTime = _currentTime.Add(timeSpan);
    }

    public void AdvanceMinutes(int minutes)
    {
        _currentTime = _currentTime.AddMinutes(minutes);
    }

    public void AdvanceHours(int hours)
    {
        _currentTime = _currentTime.AddHours(hours);
    }

    public void AdvanceDays(int days)
    {
        _currentTime = _currentTime.AddDays(days);
    }
}

