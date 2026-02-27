namespace Granit.Cookies.Options;

/// <summary>
/// Configuration options for the Granit.Cookies module.
/// </summary>
public sealed class GranitCookiesOptions
{
    /// <summary>Section key in the configuration.</summary>
    public const string SectionName = "Cookies";

    /// <summary>
    /// When <c>true</c>, writing an unregistered cookie throws <see cref="Exceptions.UnregisteredCookieException"/>.
    /// Default: <c>true</c> (Fail-Fast).
    /// </summary>
    public bool ThrowOnUnregistered { get; set; } = true;

    /// <summary>
    /// Default retention period in days for cookies that do not specify one.
    /// Default: 365.
    /// </summary>
    public int DefaultRetentionDays { get; set; } = 365;
}
