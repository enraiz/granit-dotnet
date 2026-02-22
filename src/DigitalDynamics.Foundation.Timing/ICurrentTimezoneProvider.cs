// =============================================================================
// ICurrentTimezoneProvider - Current user's timezone
// =============================================================================
// Carries the timezone of the current request via AsyncLocal.
// Allows IClock.ConvertToUserTime() to know the target timezone.
//
// The timezone is an IANA identifier (e.g. "Europe/Brussels", "America/New_York")
// or a Windows identifier (e.g. "Romance Standard Time"). TimeZoneInfo.FindSystemTimeZoneById()
// accepts both formats on .NET 6+.
//
// Inspired by Volo.Abp.Timing.ICurrentTimezoneProvider.
// =============================================================================

namespace DigitalDynamics.Foundation.Timing;

/// <summary>
/// Provides the current user's timezone (per-request via AsyncLocal).
/// </summary>
public interface ICurrentTimezoneProvider
{
    /// <summary>
    /// Identifier of the current timezone (IANA or Windows).
    /// <c>null</c> if no timezone is defined (dates remain in UTC).
    /// </summary>
    string? Timezone { get; set; }
}
