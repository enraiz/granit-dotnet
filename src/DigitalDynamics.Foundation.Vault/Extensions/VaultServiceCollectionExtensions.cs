// =============================================================================
// VaultServiceCollectionExtensions - Vault service registration
// =============================================================================
// Usage:
//   builder.Services.AddFoundationVault(builder.Configuration);
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
/// Extensions for configuring Vault services in the DI container.
/// </summary>
public static class VaultServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Vault client, the dynamic credentials lease manager,
    /// and the Transit encryption service.
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
