using Granit.Localization;
using Granit.Localization.Extensions;
using Granit.Vault.HealthChecks;
using Granit.Vault.Options;
using Granit.Vault.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using VaultSharp;

namespace Granit.Vault.Extensions;

/// <summary>
/// Extensions for configuring Vault services in the DI container.
/// </summary>
public static class VaultServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Vault client, the dynamic credentials lease manager,
    /// and the Transit encryption service.
    /// </summary>
    public static IServiceCollection AddGranitVault(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGranitLocalization();
        services.Configure<GranitLocalizationOptions>(options =>
        {
            options.Resources
                .Add<VaultLocalizationResource>(defaultCulture: "fr")
                .AddJson(
                    typeof(VaultLocalizationResource).Assembly,
                    "Granit.Vault.Localization.Vault")
                .AddBaseTypes(typeof(GranitLocalizationResource));
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

    /// <summary>
    /// Adds a Vault connectivity health check tagged <c>"readiness"</c>.
    /// Verifies the Vault <c>sys/health</c> endpoint. Returns <c>Degraded</c> for standby
    /// replicas (read-only but functional) and <c>Unhealthy</c> for sealed or unreachable Vault.
    /// </summary>
    /// <remarks>
    /// Only call this method when Vault is registered (i.e., not in Development).
    /// The <see cref="IVaultClient"/> must be registered in the DI container beforehand via
    /// <see cref="AddGranitVault"/>.
    /// </remarks>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">Check name. Defaults to <c>"vault"</c>.</param>
    /// <param name="failureStatus">Status on failure. Defaults to <see cref="HealthStatus.Unhealthy"/>.</param>
    /// <param name="timeout">Check timeout. Defaults to 10 seconds.</param>
    public static IHealthChecksBuilder AddGranitVaultCheck(
        this IHealthChecksBuilder builder,
        string name = "vault",
        HealthStatus? failureStatus = null,
        TimeSpan? timeout = null)
    {
        builder.Services.AddSingleton<VaultHealthCheck>();

        return builder.Add(new HealthCheckRegistration(
            name,
            sp => sp.GetRequiredService<VaultHealthCheck>(),
            failureStatus,
            ["readiness"],
            timeout ?? TimeSpan.FromSeconds(10)));
    }
}
