// =============================================================================
// EncryptionServiceCollectionExtensions - Service registration
// =============================================================================
// Usage:
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
/// Extensions for registering encryption services in the DI container.
/// </summary>
public static class EncryptionServiceCollectionExtensions
{
    /// <summary>
    /// Adds the string encryption service with the default AES-256-CBC provider.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="configuration">Configuration section "Encryption".</param>
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
