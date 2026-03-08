namespace Granit.Settings.Values;

/// <summary>
/// Reads setting values from the persistence store.
/// </summary>
public interface ISettingStoreReader
{
    /// <summary>
    /// Returns the stored value, or <c>null</c> if absent.
    /// </summary>
    Task<SettingValue?> GetOrNullAsync(
        string name,
        string providerName,
        string? providerKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all stored values for a given provider and key.
    /// </summary>
    Task<IReadOnlyList<SettingValue>> GetListAsync(
        string providerName,
        string? providerKey,
        CancellationToken cancellationToken = default);
}
