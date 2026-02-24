namespace Granit.Timing;

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
