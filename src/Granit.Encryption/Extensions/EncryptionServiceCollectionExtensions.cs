using Granit.Encryption.Providers;
using Granit.Encryption.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.Encryption.Extensions;

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
    public static IServiceCollection AddGranitEncryption(
        this IServiceCollection services,
        IConfigurationSection configuration)
    {
        services.Configure<StringEncryptionOptions>(configuration);

        services.AddSingleton<IStringEncryptionProvider, AesStringEncryptionProvider>();

        services.TryAddSingleton<IStringEncryptionService, DefaultStringEncryptionService>();

        return services;
    }
}
