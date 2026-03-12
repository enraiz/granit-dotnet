using Granit.Core.Modularity;
using Granit.Encryption;
using Granit.Vault.Extensions;
using Granit.Vault.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Granit.Vault;

/// <summary>
/// Module Granit pour Vault (credentials dynamiques + Transit encryption).
/// Disabled in Development (no Vault required locally).
/// Registers <see cref="VaultStringEncryptionProvider"/> when enabled.
/// </summary>
/// <remarks>
/// Localization resources (<c>Localization/Vault/{culture}.json</c>) are embedded in this
/// assembly and auto-discovered by <c>GranitLocalizationModule</c> via
/// <see cref="VaultLocalizationResource"/>.
/// </remarks>
[DependsOn(typeof(GranitEncryptionModule))]
public sealed class GranitVaultModule : GranitModule
{
    /// <inheritdoc />
    public override bool IsEnabled(ServiceConfigurationContext context) =>
        !context.Builder.Environment.IsDevelopment();

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddGranitVault();
        context.Services.AddSingleton<IStringEncryptionProvider, VaultStringEncryptionProvider>();
    }
}
