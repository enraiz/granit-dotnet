using Granit.Persistence.Extensions;
using Granit.Querying.EntityFrameworkCore.Internal;
using Granit.Querying.SavedViews;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Granit.Querying.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for registering the Querying EF Core persistence layer on <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class QueryingEfCoreHostApplicationBuilderExtensions
{
    /// <summary>
    /// Registers the Querying EF Core persistence layer, including the isolated
    /// <c>QueryingDbContext</c> and <see cref="ISavedViewStoreReader"/>/<see cref="ISavedViewStoreWriter"/> implementation.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Action to configure the database provider (e.g. <c>opts.UseNpgsql(cs)</c>).</param>
    /// <returns>The host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddGranitQueryingEntityFrameworkCore(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder> configure)
    {
        builder.Services.AddGranitDbContext<QueryingDbContext>(configure);

        // Replace the null-object default from Granit.Querying — CQRS forwarding pattern
        builder.Services.AddScoped<EfCoreSavedViewStore>();
        builder.Services.Replace(
            ServiceDescriptor.Scoped<ISavedViewStoreReader>(sp => sp.GetRequiredService<EfCoreSavedViewStore>()));
        builder.Services.Replace(
            ServiceDescriptor.Scoped<ISavedViewStoreWriter>(sp => sp.GetRequiredService<EfCoreSavedViewStore>()));

        return builder;
    }
}
