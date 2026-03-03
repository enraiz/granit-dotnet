using System.Threading.Channels;
using Granit.DataImport.Export;
using Granit.DataImport.Export.Internal;
using Granit.DataImport.Export.Messages;
using Granit.DataImport.Internal;
using Granit.DataImport.Mapping;
using Granit.DataImport.Messages;
using Granit.DataImport.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.DataImport;

/// <summary>
/// Extension methods for registering <c>Granit.DataImport</c> services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core data import pipeline infrastructure.
    /// </summary>
    /// <remarks>
    /// Registers the following services:
    /// <list type="bullet">
    ///   <item><see cref="IMappingSuggestionService"/> (scoped) — 4-tier mapping facade.</item>
    ///   <item><see cref="ISemanticMappingService"/> (singleton) — null-object default.</item>
    ///   <item><see cref="IImportJobStore"/> (scoped) — null-object default.</item>
    ///   <item><see cref="IImportFileProvider"/> (scoped) — null-object default.</item>
    ///   <item><see cref="IImportOrchestrator"/> (scoped) — pipeline orchestrator.</item>
    ///   <item><see cref="IImportCommandDispatcher"/> (singleton) — channel-based dispatch.</item>
    /// </list>
    /// <para>
    /// At least one <see cref="Parsing.IFileParser"/> must be registered separately.
    /// Use <c>Granit.DataImport.Csv</c> or <c>Granit.DataImport.Excel</c>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitDataImport(this IServiceCollection services)
    {
        services.AddOptions<DataImportOptions>()
            .BindConfiguration(DataImportOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton<ISemanticMappingService, NullSemanticMappingService>();
        services.TryAddScoped<IMappingSuggestionService, MappingSuggestionService>();
        services.TryAddScoped<IImportJobStore, NullImportJobStore>();
        services.TryAddScoped<IImportFileProvider, NullImportFileProvider>();
        services.TryAddScoped<IImportOrchestrator, ImportOrchestrator>();

        // Channel-based async dispatch (default). Replaced by Wolverine if installed.
        services.TryAddSingleton(Channel.CreateUnbounded<ExecuteImportCommand>());
        services.TryAddSingleton<IImportCommandDispatcher, ChannelImportCommandDispatcher>();
        services.AddHostedService<ImportCommandWorker>();

        return services;
    }

    /// <summary>
    /// Registers an import definition for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <typeparam name="TDefinition">The import definition implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddImportDefinition<TEntity, TDefinition>(
        this IServiceCollection services)
        where TEntity : class
        where TDefinition : ImportDefinition<TEntity>
    {
        services.AddSingleton<ImportDefinition<TEntity>, TDefinition>();
        services.AddSingleton<IImportDefinitionDescriptor>(sp =>
            sp.GetRequiredService<ImportDefinition<TEntity>>());
        return services;
    }

    /// <summary>
    /// Registers the core data export pipeline infrastructure.
    /// </summary>
    /// <remarks>
    /// Registers the following services:
    /// <list type="bullet">
    ///   <item><see cref="IExportOrchestrator"/> (scoped) — export pipeline orchestrator.</item>
    ///   <item><see cref="IExportJobStore"/> (scoped) — null-object default.</item>
    ///   <item><see cref="IExportPresetStore"/> (scoped) — null-object default.</item>
    ///   <item><see cref="IExportCommandDispatcher"/> (singleton) — channel-based dispatch.</item>
    /// </list>
    /// <para>
    /// At least one <see cref="IExportWriter"/> must be registered separately.
    /// Use <c>Granit.DataImport.Excel</c> or <c>Granit.DataImport.Csv</c>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitDataExport(this IServiceCollection services)
    {
        services.AddOptions<ExportOptions>()
            .BindConfiguration(ExportOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddScoped<IExportOrchestrator, ExportOrchestrator>();
        services.TryAddScoped<IExportJobStore, NullExportJobStore>();
        services.TryAddScoped<IExportPresetStore, NullExportPresetStore>();

        // Channel-based async dispatch (default). Replaced by Wolverine if installed.
        services.TryAddSingleton(Channel.CreateUnbounded<ExecuteExportCommand>());
        services.TryAddSingleton<IExportCommandDispatcher, ChannelExportCommandDispatcher>();
        services.AddHostedService<ExportCommandWorker>();

        return services;
    }

    /// <summary>
    /// Registers an export definition with a typed filter for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The source entity type.</typeparam>
    /// <typeparam name="TFilter">The filter type.</typeparam>
    /// <typeparam name="TDefinition">The export definition implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExportDefinition<TEntity, TFilter, TDefinition>(
        this IServiceCollection services)
        where TEntity : class
        where TFilter : class
        where TDefinition : ExportDefinition<TEntity, TFilter>
    {
        services.AddSingleton<ExportDefinition<TEntity, TFilter>, TDefinition>();
        services.AddSingleton<IExportDefinitionDescriptor>(sp =>
            sp.GetRequiredService<ExportDefinition<TEntity, TFilter>>());
        return services;
    }

    /// <summary>
    /// Registers an export definition without filtering for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The source entity type.</typeparam>
    /// <typeparam name="TDefinition">The export definition implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExportDefinition<TEntity, TDefinition>(
        this IServiceCollection services)
        where TEntity : class
        where TDefinition : ExportDefinition<TEntity> =>
        services.AddExportDefinition<TEntity, EmptyExportFilter, TDefinition>();

    /// <summary>
    /// Replaces the default <see cref="ISemanticMappingService"/> with an AI-backed implementation.
    /// </summary>
    /// <typeparam name="TService">The semantic mapping service implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSemanticMappingService<TService>(
        this IServiceCollection services)
        where TService : class, ISemanticMappingService
    {
        services.Replace(ServiceDescriptor.Singleton<ISemanticMappingService, TService>());
        return services;
    }
}
