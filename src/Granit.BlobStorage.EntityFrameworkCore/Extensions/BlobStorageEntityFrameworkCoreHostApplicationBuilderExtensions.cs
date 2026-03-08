using Granit.BlobStorage.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Granit.BlobStorage.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for registering EF Core persistence for Granit blob storage.
/// </summary>
public static class BlobStorageEntityFrameworkCoreHostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers EF Core persistence for Granit blob storage.
    /// </summary>
    /// <remarks>
    /// Registers <see cref="BlobStorageDbContext"/> via
    /// <see cref="EntityFrameworkServiceCollectionExtensions.AddDbContextFactory{TContext}(IServiceCollection, Action{DbContextOptionsBuilder}?, ServiceLifetime)"/>
    /// and binds <see cref="IBlobDescriptorStore"/> to <c>EfBlobDescriptorStore</c>.
    /// <para>
    /// Must be called after <c>AddGranitBlobStorageS3()</c> (or any other blob storage provider).
    /// </para>
    /// <para>
    /// The connection string must point to a database hosted in Europe (OVHcloud FR).
    /// Never use a service subject to the US Cloud Act for health data.
    /// </para>
    /// </remarks>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">EF Core <see cref="DbContextOptionsBuilder"/> configuration (provider + connection string).</param>
    /// <returns>The builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitBlobStorageEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configure)
    {
        builder.Services.AddDbContextFactory<BlobStorageDbContext>(configure);
        builder.Services.AddScoped<EfBlobDescriptorStore>();
        builder.Services.AddScoped<IBlobDescriptorStore>(sp => sp.GetRequiredService<EfBlobDescriptorStore>());
        builder.Services.AddScoped<IBlobDescriptorReader>(sp => sp.GetRequiredService<EfBlobDescriptorStore>());
        builder.Services.AddScoped<IBlobDescriptorWriter>(sp => sp.GetRequiredService<EfBlobDescriptorStore>());

        return builder;
    }
}
