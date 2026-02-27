using Granit.Core.Modularity;
using Granit.Encryption;
using Granit.Vault.Extensions;
using Granit.Vault.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Granit.Vault;

/// <summary>
/// Module Granit pour Vault (credentials dynamiques + Transit encryption).
/// Skip l'enregistrement en Development (pas de Vault en local).
/// Enregistre <see cref="VaultStringEncryptionProvider"/> si Vault est actif.
/// </summary>
/// <remarks>
/// Localization resources (<c>Localization/Vault/{culture}.json</c>) are embedded in this
/// assembly and auto-discovered by <c>GranitLocalizationModule</c> via
/// <see cref="VaultLocalizationResource"/>.
/// </remarks>
[DependsOn(typeof(GranitEncryptionModule))]
public sealed class GranitVaultModule : GranitModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        if (context.Builder.Environment.IsDevelopment())
        {
            return;
        }

        context.Services.AddGranitVault();

        context.Services.AddSingleton<IStringEncryptionProvider, VaultStringEncryptionProvider>();
    }
}
