using Granit.Caching;
using Granit.Core.Modularity;
using Granit.Encryption;
using Granit.Security;
using Granit.Settings.Extensions;
using Granit.Settings.Options;

namespace Granit.Settings;

/// <summary>
/// Granit module for dynamic settings management with cascading resolution.
/// </summary>
[DependsOn(typeof(GranitCachingModule))]
[DependsOn(typeof(GranitEncryptionModule))]
[DependsOn(typeof(GranitSecurityModule))]
public sealed class GranitSettingsModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitSettings(
            context.Configuration.GetSection(SettingsOptions.SectionName));
}
