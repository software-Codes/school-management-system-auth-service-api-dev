namespace AuthService.Abstractions.Common;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
