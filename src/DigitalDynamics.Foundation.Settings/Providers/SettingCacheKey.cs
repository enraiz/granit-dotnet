namespace DigitalDynamics.Foundation.Settings.Providers;

/// <summary>
/// Builds cache keys for setting values.
/// </summary>
internal static class SettingCacheKey
{
    internal static string Build(string providerName, string? providerKey, string name) =>
        $"{providerName}:{providerKey ?? "global"}:{name}";
}
