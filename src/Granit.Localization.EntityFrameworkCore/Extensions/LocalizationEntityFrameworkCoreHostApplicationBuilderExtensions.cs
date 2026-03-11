using Granit.Localization.EntityFrameworkCore.Internal;
using Granit.Localization.Internal;
using Granit.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Granit.Localization.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for registering EF Core persistence for Granit localization overrides.
/// </summary>
public static class LocalizationEntityFrameworkCoreHostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers EF Core persistence for Granit localization overrides.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers <see cref="EfCoreLocalizationOverrideStore"/> as a keyed Scoped service
    /// (<see cref="CachedLocalizationOverrideStore.RawStoreKey"/>). The Singleton
    /// <see cref="CachedLocalizationOverrideStore"/> — registered by
    /// <c>GranitLocalizationModule</c> — resolves it via
    /// <c>IServiceScopeFactory</c> per DB operation, ensuring ISO 27001 audit compliance
    /// through <see cref="AuditedEntityInterceptor"/> on write operations.
    /// </para>
    /// <para>
    /// Must be called after the module system has been initialized (i.e. after
    /// <c>GranitLocalizationEntityFrameworkCoreModule</c> is loaded).
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">EF Core <see cref="DbContextOptionsBuilder"/> configuration (provider + connection string).</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitLocalizationEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configure)
    {
        builder.Services.AddDbContextFactory<GranitLocalizationOverridesDbContext>((sp, options) =>
        {
            configure(options);

            // Automatically wire the ISO 27001 audit interceptor when Granit.Persistence is present.
            // The interceptor is Scoped — using ServiceLifetime.Scoped for the factory ensures
            // it is resolved from the current request/message scope on write operations.
            AuditedEntityInterceptor? auditInterceptor =
                sp.GetService<AuditedEntityInterceptor>();
            if (auditInterceptor is not null)
            {
                options.AddInterceptors(auditInterceptor);
            }
        }, ServiceLifetime.Scoped);

        builder.Services.TryAddKeyedScoped<ILocalizationOverrideStoreReader, EfCoreLocalizationOverrideStore>(
            CachedLocalizationOverrideStore.RawStoreKey);
        builder.Services.TryAddKeyedScoped<ILocalizationOverrideStoreWriter, EfCoreLocalizationOverrideStore>(
            CachedLocalizationOverrideStore.RawStoreKey);

        return builder;
    }
}
