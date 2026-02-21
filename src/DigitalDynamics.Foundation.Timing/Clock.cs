// =============================================================================
// Clock - Implementation par defaut de IClock
// =============================================================================
// Delegue a System.TimeProvider pour Now (testable via FakeTimeProvider).
// Ajoute les conversions de fuseau horaire via ICurrentTimezoneProvider.
//
// Enregistre en Singleton : TimeProvider.System est thread-safe et stateless,
// et CurrentTimezoneProvider utilise AsyncLocal pour l'isolation per-request.
//
// Performance : TimeZoneInfo.FindSystemTimeZoneById peut etre couteux si appele
// des milliers de fois. Envisager un cache Dictionary si les tests de charge
// montrent une regression (YAGNI pour l'instant).
// =============================================================================

namespace DigitalDynamics.Foundation.Timing;

/// <summary>
/// Implementation par defaut de <see cref="IClock"/>.
/// Utilise <see cref="TimeProvider"/> pour l'acces au temps
/// et <see cref="ICurrentTimezoneProvider"/> pour les conversions timezone.
/// </summary>
public sealed class Clock : IClock
{
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentTimezoneProvider _timezoneProvider;

    public Clock(TimeProvider timeProvider, ICurrentTimezoneProvider timezoneProvider)
    {
        _timeProvider = timeProvider;
        _timezoneProvider = timezoneProvider;
    }

    /// <inheritdoc />
    public DateTimeOffset Now => _timeProvider.GetUtcNow();

    /// <inheritdoc />
    public bool SupportsMultipleTimezone => true;

    /// <inheritdoc />
    public DateTimeOffset Normalize(DateTimeOffset dateTime)
    {
        // Conformite HDS : tout est converti en UTC avant persistance.
        // Meme un DateTimeOffset avec offset local (+02:00) sera normalise en UTC (+00:00).
        return dateTime.ToUniversalTime();
    }

    /// <inheritdoc />
    public DateTimeOffset ConvertToUserTime(DateTimeOffset utcDateTime)
    {
        var tz = _timezoneProvider.Timezone;
        if (string.IsNullOrWhiteSpace(tz))
        {
            return utcDateTime;
        }

        var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
        return TimeZoneInfo.ConvertTime(utcDateTime, tzInfo);
    }

    /// <inheritdoc />
    public DateTimeOffset ConvertToUtc(DateTimeOffset dateTime)
    {
        return dateTime.ToUniversalTime();
    }
}
