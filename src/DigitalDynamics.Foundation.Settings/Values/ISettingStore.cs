// =============================================================================
// ISettingStore - Persistence abstraction for setting values
// =============================================================================
// Low-level interface: no cache, no application-level encryption.
// Encryption of IsEncrypted values is handled at this layer (EF store).
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Values;

/// <summary>
/// Persistence abstraction for setting values.
/// Encryption of sensitive settings is handled by the implementation.
/// </summary>
public interface ISettingStore
{
    /// <summary>
    /// Returns the stored value, or <c>null</c> if absent.
    /// </summary>
    Task<SettingValue?> GetOrNullAsync(
        string name,
        string providerName,
        string? providerKey,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all stored values for a given provider and key.
    /// </summary>
    Task<IReadOnlyList<SettingValue>> GetListAsync(
        string providerName,
        string? providerKey,
        CancellationToken ct = default);

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
