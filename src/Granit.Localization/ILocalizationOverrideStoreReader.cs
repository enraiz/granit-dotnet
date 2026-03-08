namespace Granit.Localization;

/// <summary>
/// Read-side contract for per-resource, per-culture translation overrides.
/// </summary>
public interface ILocalizationOverrideStoreReader
{
    /// <summary>
    /// Returns all overrides for the given resource and culture as a key-value dictionary.
    /// Returns an empty dictionary when no overrides are defined.
    /// </summary>
    /// <param name="resourceName">Logical name of the localization resource (e.g. <c>"Acme"</c>).</param>
    /// <param name="culture">BCP 47 culture tag (e.g. <c>"fr"</c>, <c>"en-US"</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyDictionary<string, string>> GetOverridesAsync(
        string resourceName, string culture, CancellationToken cancellationToken = default);
}
