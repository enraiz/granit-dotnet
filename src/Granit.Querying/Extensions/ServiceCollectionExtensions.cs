using Granit.Querying.Options;
using Granit.Querying.SavedViews;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Granit.Querying.Extensions;

/// <summary>
/// Extension methods for registering <c>Granit.Querying</c> services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core querying infrastructure.
    /// </summary>
    /// <remarks>
    /// Registers the following services:
    /// <list type="bullet">
    ///   <item><see cref="ISavedViewStoreReader"/> / <see cref="ISavedViewStoreWriter"/> (scoped) — null-object default.</item>
    /// </list>
    /// <para>
    /// For the EF Core query engine, add <c>Granit.Querying.EntityFrameworkCore</c>.
    /// For REST endpoints, add <c>Granit.Querying.Endpoints</c>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitQuerying(this IServiceCollection services)
    {
        // Default in-memory store — register concrete type, then forward both interfaces to same instance.
        services.TryAddScoped<NullSavedViewStore>();
        services.TryAddScoped<ISavedViewStoreReader>(sp => sp.GetRequiredService<NullSavedViewStore>());
        services.TryAddScoped<ISavedViewStoreWriter>(sp => sp.GetRequiredService<NullSavedViewStore>());

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<IOptions<QueryingOptions>>().Value);

        return services;
    }

    /// <summary>
    /// Registers a query definition for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <typeparam name="TDefinition">The query definition implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddQueryDefinition<TEntity, TDefinition>(
        this IServiceCollection services)
        where TEntity : class
        where TDefinition : QueryDefinition<TEntity>, new()
    {
        services.AddSingleton<QueryDefinition<TEntity>>(sp =>
        {
            TDefinition definition = new();
            QueryingOptions options = sp.GetService<QueryingOptions>() ?? new();
            definition.Initialize(options);
            return definition;
        });
        services.AddSingleton<IQueryDefinitionDescriptor>(sp =>
            sp.GetRequiredService<QueryDefinition<TEntity>>());
        return services;
    }
}
