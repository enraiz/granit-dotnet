// =============================================================================
// EncryptionServiceCollectionExtensions - Enregistrement des services
// =============================================================================
// Usage :
//   builder.Services.AddFoundationEncryption(
//       builder.Configuration.GetSection(StringEncryptionOptions.SectionName));
// =============================================================================

using DigitalDynamics.Foundation.Encryption.Providers;
using DigitalDynamics.Foundation.Encryption.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DigitalDynamics.Foundation.Encryption.Extensions;

/// <summary>
/// Extensions pour configurer les services de chiffrement dans le conteneur DI.
/// </summary>
public static class EncryptionServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute le service de chiffrement de chaînes avec le provider AES-256-CBC par défaut.
    /// </summary>
    /// <param name="services">Conteneur DI.</param>
    /// <param name="configuration">Section de configuration "Encryption".</param>
    public static IServiceCollection AddFoundationEncryption(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        services.Configure<StringEncryptionOptions>(configuration);

        services.AddSingleton<IStringEncryptionProvider, AesStringEncryptionProvider>();

        services.TryAddSingleton<IStringEncryptionService, DefaultStringEncryptionService>();

        return services;
    }
}
