using Granit.Settings.EntityFrameworkCore.Internal;
using Granit.Settings.Values;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Granit.Settings.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for registering EF Core persistence for Granit settings.
/// </summary>
public static class SettingsEntityFrameworkCoreHostApplicationBuilderExtensions
{
    /// <summary>
    /// Replaces the default <c>InMemorySettingStore</c> with <see cref="EfCoreSettingStore{TDbContext}"/>,
    /// persisting setting values in the host application's existing DbContext.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <typeparamref name="TDbContext"/> must implement <see cref="ISettingsDbContext"/>
    /// and call <c>modelBuilder.ConfigureSettingsModule()</c> in <c>OnModelCreating</c>.
    /// </para>
    /// <para>
    /// The <c>AuditedEntityInterceptor</c> from <c>Granit.Persistence</c> must be wired
    /// to <typeparamref name="TDbContext"/> by the host application to ensure the HDS
    /// 3-year audit trail is populated on every write.
    /// </para>
    /// <para>
    /// The connection string must point to a database hosted in Europe (OVHcloud FR).
    /// Never use a service subject to the US Cloud Act for health data.
    /// </para>
    /// </remarks>
    /// <typeparam name="TDbContext">
    /// The host application's DbContext implementing <see cref="ISettingsDbContext"/>.
    /// </typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitSettingsEfCore<TDbContext>(
        this IHostApplicationBuilder builder)
        where TDbContext : DbContext, ISettingsDbContext
    {
        builder.Services.Replace(ServiceDescriptor.Singleton<ISettingStoreReader>(sp =>
            new EfCoreSettingStore<TDbContext>(
                sp.GetRequiredService<IServiceScopeFactory>())));
        builder.Services.Replace(ServiceDescriptor.Singleton<ISettingStoreWriter>(sp =>
            new EfCoreSettingStore<TDbContext>(
                sp.GetRequiredService<IServiceScopeFactory>())));

        return builder;
    }
}
