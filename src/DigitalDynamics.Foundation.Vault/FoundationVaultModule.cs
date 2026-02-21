// =============================================================================
// FoundationVaultModule - Module Foundation pour Vault
// =============================================================================
// Skip automatiquement l'enregistrement en environnement Development
// (credentials statiques via connection string).
// =============================================================================

using DigitalDynamics.Foundation.Core.Modularity;
using DigitalDynamics.Foundation.Vault.Extensions;
using Microsoft.Extensions.Hosting;

namespace DigitalDynamics.Foundation.Vault;

/// <summary>
/// Module Foundation pour Vault (credentials dynamiques + Transit encryption).
/// Skip l'enregistrement en Development (pas de Vault en local).
/// </summary>
public sealed class FoundationVaultModule : FoundationModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        if (context.Builder.Environment.IsDevelopment())
        {
            return;
        }

        context.Services.AddFoundationVault(context.Configuration);
    }
}
