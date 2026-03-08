namespace Granit.Settings.Values;

/// <summary>
/// Writes setting values to the persistence store.
/// Encryption of sensitive settings is handled by the implementation.
/// </summary>
public interface ISettingStoreWriter
{
    /// <summary>
    /// Creates or updates the value of a setting.
    /// </summary>
    Task SetAsync(
        string name,
        string providerName,
        string? providerKey,
        string? value,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes the value of a setting for a given provider and key.
    /// </summary>
    Task DeleteAsync(
        string name,
        string providerName,
        string? providerKey,
        CancellationToken ct = default);
}
