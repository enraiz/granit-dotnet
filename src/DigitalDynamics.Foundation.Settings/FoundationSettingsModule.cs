using DigitalDynamics.Foundation.Caching;
using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Encryption;
using DigitalDynamics.Foundation.MultiTenancy;
using DigitalDynamics.Foundation.Security;
using DigitalDynamics.Foundation.Settings.Extensions;
using DigitalDynamics.Foundation.Settings.Options;

namespace DigitalDynamics.Foundation.Settings;

/// <summary>
/// Foundation module for dynamic settings management with cascading resolution.
/// </summary>
[DependsOn(typeof(FoundationCachingModule))]
[DependsOn(typeof(FoundationMultiTenancyModule))]
[DependsOn(typeof(FoundationEncryptionModule))]
[DependsOn(typeof(FoundationSecurityModule))]
public sealed class FoundationSettingsModule : FoundationModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddFoundationSettings(
            context.Configuration.GetSection(SettingsOptions.SectionName));
}
