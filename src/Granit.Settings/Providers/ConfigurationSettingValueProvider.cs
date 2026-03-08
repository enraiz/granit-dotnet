using Granit.Settings.Definitions;
using Granit.Settings.Values;
using Microsoft.Extensions.Configuration;

namespace Granit.Settings.Providers;

/// <summary>
/// Settings provider from <see cref="IConfiguration"/>.
/// Reads the <c>Settings:{name}</c> section (order = 400, before Default).
/// </summary>
public sealed class ConfigurationSettingValueProvider(IConfiguration configuration) : ISettingValueProvider
{
    /// <summary>Configuration provider identifier.</summary>
    public const string ProviderName = "C";

    private readonly IConfiguration _configuration = configuration;

    /// <inheritdoc/>
    public string Name => ProviderName;

    /// <inheritdoc/>
    public int Order => 400;

    /// <inheritdoc/>
    public Task<SettingValue?> GetOrNullAsync(SettingDefinition definition, CancellationToken cancellationToken = default)
    {
        string? value = _configuration[$"Settings:{definition.Name}"];
        SettingValue? result = value is not null
            ? new SettingValue(definition.Name, ProviderName, null, value)
            : null;
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task SetAsync(SettingDefinition definition, string? value, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task ClearAsync(SettingDefinition definition, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
