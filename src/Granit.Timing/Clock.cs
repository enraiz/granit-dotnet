namespace Granit.Timing;

/// <summary>
/// Implementation par defaut de <see cref="IClock"/>.
/// Utilise <see cref="TimeProvider"/> pour l'acces au temps
/// et <see cref="ICurrentTimezoneProvider"/> pour les conversions timezone.
/// </summary>
public sealed class Clock(TimeProvider timeProvider, ICurrentTimezoneProvider timezoneProvider) : IClock
{
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ICurrentTimezoneProvider _timezoneProvider = timezoneProvider;

    /// <inheritdoc />
    public DateTimeOffset Now => _timeProvider.GetUtcNow();

    /// <inheritdoc />
    public bool SupportsMultipleTimezone => true;

    /// <inheritdoc />
    public DateTimeOffset Normalize(DateTimeOffset dateTime) =>
        // Conformite HDS : tout est converti en UTC avant persistance.
        // Meme un DateTimeOffset avec offset local (+02:00) sera normalise en UTC (+00:00).
        dateTime.ToUniversalTime();

    /// <inheritdoc />
    public DateTimeOffset ConvertToUserTime(DateTimeOffset utcDateTime)
    {
        string? tz = _timezoneProvider.Timezone;
        if (string.IsNullOrWhiteSpace(tz))
        {
            return utcDateTime;
        }

        TimeZoneInfo tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
        return TimeZoneInfo.ConvertTime(utcDateTime, tzInfo);
    }

    /// <inheritdoc />
    public DateTimeOffset ConvertToUtc(DateTimeOffset dateTime) => dateTime.ToUniversalTime();
}
