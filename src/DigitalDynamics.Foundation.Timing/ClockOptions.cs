namespace DigitalDynamics.Foundation.Timing;

/// <summary>
/// Configuration options for the Timing module.
/// </summary>
public sealed class ClockOptions
{
    /// <summary>
    /// Default timezone when none is specified by the user
    /// via <see cref="Abstractions.Timing.ICurrentTimezoneProvider"/>.
    /// <c>null</c> = no conversion (dates remain in UTC).
    /// Example: <c>"Europe/Brussels"</c>, <c>"America/New_York"</c>
    /// </summary>
    public string? DefaultTimezone { get; set; }
}
