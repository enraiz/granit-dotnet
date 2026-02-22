// =============================================================================
// SettingValueProviderManager - Ordered manager of value providers
// =============================================================================
// Aggregates all ISettingValueProvider instances registered in DI and sorts them
// in ascending order (100 = highest priority, 500 = last resort).
//
// Default order:
//   User (U, 100) → Tenant (T, 200) → Global (G, 300)
//   → Configuration (C, 400) → Default (D, 500)
//
// Inputs  : IEnumerable<ISettingValueProvider> (DI)
// Outputs : Sorted providers (access by list or by name)
// =============================================================================

namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Manager of setting value providers, sorted by priority order.
/// </summary>
public sealed class SettingValueProviderManager
{
    /// <summary>
    /// Ordered list of providers, from highest priority (U=100) to lowest priority (D=500).
    /// </summary>
    public IReadOnlyList<ISettingValueProvider> Providers { get; }

    public SettingValueProviderManager(IEnumerable<ISettingValueProvider> providers) =>
        Providers = [.. providers.OrderBy(p => p.Order)];

    /// <summary>
    /// Returns the provider by its short name, or <c>null</c> if not registered.
    /// </summary>
    public ISettingValueProvider? GetOrNull(string name) =>
        Providers.FirstOrDefault(p => p.Name == name);
}
