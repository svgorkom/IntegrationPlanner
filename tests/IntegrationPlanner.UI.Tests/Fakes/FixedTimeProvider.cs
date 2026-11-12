namespace IntegrationPlanner.UI.Tests.Fakes;

internal sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateOnly day)
    {
        _now = MorningOf(day);
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => _now;

    public void MoveTo(DateOnly day) => _now = MorningOf(day);

    private static DateTimeOffset MorningOf(DateOnly day) => new(day.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
}
