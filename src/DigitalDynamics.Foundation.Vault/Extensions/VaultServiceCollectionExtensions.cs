// =============================================================================
// VaultServiceCollectionExtensions - Enregistrement des services Vault
// =============================================================================

using DigitalDynamics.Foundation.Localization;
using DigitalDynamics.Foundation.Localization.Extensions;
using DigitalDynamics.Foundation.Vault.Options;
using DigitalDynamics.Foundation.Vault.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VaultSharp;

namespace DigitalDynamics.Foundation.Vault.Extensions;

/// <summary>
/// Extensions pour configurer les services Vault dans le conteneur DI.
/// </summary>
public static class VaultServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute le client Vault, le gestionnaire de credentials dynamiques
    /// et le service de chiffrement Transit.
    /// </summary>
    public static IServiceCollection AddFoundationVault(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFoundationLocalization();
        services.Configure<FoundationLocalizationOptions>(options =>
        {
            options.Resources
                .Add<VaultLocalizationResource>(defaultCulture: "fr")
                .AddJson(
                    typeof(VaultLocalizationResource).Assembly,
                    "DigitalDynamics.Foundation.Vault.Localization.Vault")
                .AddBaseTypes(typeof(FoundationLocalizationResource));
        });

        services.Configure<VaultOptions>(configuration.GetSection(VaultOptions.SectionName));

        services.AddSingleton<VaultClientFactory>();
        services.AddSingleton<IVaultClient>(sp => sp.GetRequiredService<VaultClientFactory>().Create());

        services.AddSingleton<VaultCredentialLeaseManager>();
        services.AddSingleton<IDatabaseCredentialProvider>(sp =>
            sp.GetRequiredService<VaultCredentialLeaseManager>());
        services.AddHostedService(sp => sp.GetRequiredService<VaultCredentialLeaseManager>());

        services.AddScoped<ITransitEncryptionService, TransitEncryptionService>();

        return services;
    }
}
