namespace Granit.Localization;

/// <summary>
/// Write-side contract for per-resource, per-culture translation overrides.
/// </summary>
public interface ILocalizationOverrideStoreWriter
{
    /// <summary>
    /// Creates or updates the override for a single key (upsert semantics).
    /// </summary>
    /// <param name="resourceName">Logical name of the localization resource.</param>
    /// <param name="culture">BCP 47 culture tag.</param>
    /// <param name="key">Translation key to override.</param>
    /// <param name="value">Override value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetOverrideAsync(
        string resourceName, string culture, string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the override for a single key. No-op if the key does not exist.
    /// </summary>
    /// <param name="resourceName">Logical name of the localization resource.</param>
    /// <param name="culture">BCP 47 culture tag.</param>
    /// <param name="key">Translation key to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveOverrideAsync(
        string resourceName, string culture, string key, CancellationToken cancellationToken = default);
}
