namespace Granit.Localization;

/// <summary>
/// Contract for reading and writing per-resource, per-culture translation overrides.
/// </summary>
/// <remarks>
/// Overrides take priority over embedded JSON files. An override is identified by
/// the triple (resourceName, culture, key) and scoped to a tenant when multi-tenancy
/// is active.
/// </remarks>
public interface ILocalizationOverrideStore
{
    /// <summary>
    /// Returns all overrides for the given resource and culture as a key-value dictionary.
    /// Returns an empty dictionary when no overrides are defined.
    /// </summary>
    /// <param name="resourceName">Logical name of the localization resource (e.g. <c>"Guava"</c>).</param>
    /// <param name="culture">BCP 47 culture tag (e.g. <c>"fr"</c>, <c>"en-US"</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyDictionary<string, string>> GetOverridesAsync(
        string resourceName, string culture, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates the override for a single key (upsert semantics).
    /// </summary>
    /// <param name="resourceName">Logical name of the localization resource.</param>
    /// <param name="culture">BCP 47 culture tag.</param>
    /// <param name="key">Translation key to override.</param>
    /// <param name="value">Override value.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetOverrideAsync(
        string resourceName, string culture, string key, string value, CancellationToken ct = default);

    /// <summary>
    /// Removes the override for a single key. No-op if the key does not exist.
    /// </summary>
    /// <param name="resourceName">Logical name of the localization resource.</param>
    /// <param name="culture">BCP 47 culture tag.</param>
    /// <param name="key">Translation key to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RemoveOverrideAsync(
        string resourceName, string culture, string key, CancellationToken ct = default);
}
