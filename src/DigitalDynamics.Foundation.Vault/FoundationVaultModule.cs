// =============================================================================
// FoundationVaultModule - Module Foundation pour Vault
// =============================================================================
// Skip automatiquement l'enregistrement en environnement Development
// (credentials statiques via connection string).
// Enregistre VaultStringEncryptionProvider si Vault est actif.
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Encryption;
using DigitalDynamics.Foundation.Localization;
using DigitalDynamics.Foundation.Vault.Extensions;
using DigitalDynamics.Foundation.Vault.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DigitalDynamics.Foundation.Vault;

/// <summary>
/// Module Foundation pour Vault (credentials dynamiques + Transit encryption).
/// Skip l'enregistrement en Development (pas de Vault en local).
/// Enregistre <see cref="VaultStringEncryptionProvider"/> si Vault est actif.
/// </summary>
[DependsOn(typeof(FoundationLocalizationModule))]
[DependsOn(typeof(FoundationEncryptionModule))]
public sealed class FoundationVaultModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        if (context.Builder.Environment.IsDevelopment())
        {
            return;
        }

        context.Services.AddFoundationVault(context.Configuration);

        context.Services.AddSingleton<IStringEncryptionProvider, VaultStringEncryptionProvider>();
    }
}
