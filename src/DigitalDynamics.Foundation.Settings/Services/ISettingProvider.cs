using DigitalDynamics.Foundation.Settings.Values;

namespace DigitalDynamics.Foundation.Settings.Services;

/// <summary>
/// Setting read service with automatic cascading resolution.
/// </summary>
public interface ISettingProvider
{
    /// <summary>
    /// Returns the resolved setting value, or <c>null</c> if no provider supplies a value.
    /// </summary>
    /// <param name="name">Setting name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string?> GetOrNullAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Returns the resolved values for a list of settings.
    /// </summary>
    /// <param name="names">Names of the settings to resolve.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<SettingValue>> GetAllAsync(string[] names, CancellationToken ct = default);
}
