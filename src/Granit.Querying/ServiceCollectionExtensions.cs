using Granit.Querying.SavedViews;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Granit.Querying;

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
    ///   <item><see cref="ISavedViewStore"/> (scoped) — null-object default.</item>
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
        services.TryAddScoped<ISavedViewStore, NullSavedViewStore>();

        // Register QueryingOptions with defaults from QueryingDefaults.
        // Consuming applications can override via:
        //   services.Configure<QueryingOptions>(config.GetSection("Querying"));
        //   services.Configure<QueryingOptions>(o => o.DefaultPageSize = 50);
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
            QueryingOptions options = sp.GetRequiredService<QueryingOptions>();
            definition.Initialize(options);
            return definition;
        });
        services.AddSingleton<IQueryDefinitionDescriptor>(sp =>
            sp.GetRequiredService<QueryDefinition<TEntity>>());
        return services;
    }
}
