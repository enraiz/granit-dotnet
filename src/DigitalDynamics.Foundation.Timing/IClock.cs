<<<<<<< HEAD
=======
// =============================================================================
// IClock - Abstraction pour l'acces au temps systeme
// =============================================================================
// Remplace tout appel direct a DateTimeOffset.UtcNow dans le code applicatif.
// Utilise System.TimeProvider en interne pour Now.
// Ajoute les conversions de fuseau horaire par-dessus.
//
// Usage : injecter IClock dans les handlers, intercepteurs, services.
//   var now = clock.Now;
//   var userTime = clock.ConvertToUserTime(utcDateTime);
// =============================================================================

>>>>>>> feature/settings-module
namespace DigitalDynamics.Foundation.Timing;

/// <summary>
/// Abstraction for accessing system time and performing timezone conversions.
/// Uses <see cref="TimeProvider"/> internally for <see cref="Now"/>.
/// </summary>
public interface IClock
{
    /// <summary>Gets the current instant in UTC.</summary>
    DateTimeOffset Now { get; }

    /// <summary>
    /// Indicates whether the Clock supports multiple timezones.
    /// Returns <c>true</c> when the Clock operates in UTC (standard HDS case).
    /// </summary>
    bool SupportsMultipleTimezone { get; }

    /// <summary>
    /// Normalizes a <see cref="DateTimeOffset"/> to UTC.
    /// Ensures that even a local offset (+02:00) is converted to UTC (+00:00)
    /// before persistence.
    /// </summary>
    DateTimeOffset Normalize(DateTimeOffset dateTime);

    /// <summary>
    /// Converts a UTC <see cref="DateTimeOffset"/> to the current user's timezone
    /// (via <see cref="ICurrentTimezoneProvider"/>).
    /// If no timezone is configured, returns the value unchanged.
    /// </summary>
    DateTimeOffset ConvertToUserTime(DateTimeOffset utcDateTime);

    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> from the user's timezone to UTC.
    /// If no timezone is configured, returns the value unchanged.
    /// </summary>
    DateTimeOffset ConvertToUtc(DateTimeOffset dateTime);
}
