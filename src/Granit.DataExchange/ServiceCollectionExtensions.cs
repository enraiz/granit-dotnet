using System.Threading.Channels;
using Granit.DataExchange.Export;
using Granit.DataExchange.Export.Internal;
using Granit.DataExchange.Export.Messages;
using Granit.DataExchange.Import.Internal;
using Granit.DataExchange.Import.Mapping;
using Granit.DataExchange.Import.Messages;
using Granit.DataExchange.Import.Pipeline;
using Granit.DataExchange.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Granit.DataExchange;

/// <summary>
/// Extension methods for registering <c>Granit.DataExchange</c> services.
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
    /// Use <c>Granit.DataExchange.Csv</c> or <c>Granit.DataExchange.Excel</c>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitDataImport(this IServiceCollection services)
    {
        services.AddOptions<ImportOptions>()
            .BindConfiguration(ImportOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton<ISemanticMappingService, NullSemanticMappingService>();
        services.TryAddScoped<IMappingSuggestionService, MappingSuggestionService>();
        services.TryAddScoped<IImportJobStore, NullImportJobStore>();
        services.TryAddScoped<IImportFileProvider, NullImportFileProvider>();
        services.TryAddScoped<IImportOrchestrator, ImportOrchestrator>();

        // No-op event publisher (default). Replaced by Wolverine if installed.
        services.TryAddSingleton<IDataExchangeEventPublisher, NullDataExchangeEventPublisher>();

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
    /// Use <c>Granit.DataExchange.Excel</c> or <c>Granit.DataExchange.Csv</c>.
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

        // No-op event publisher (default). Replaced by Wolverine if installed.
        services.TryAddSingleton<IDataExchangeEventPublisher, NullDataExchangeEventPublisher>();

        // Channel-based async dispatch (default). Replaced by Wolverine if installed.
        services.TryAddSingleton(Channel.CreateUnbounded<ExecuteExportCommand>());
        services.TryAddSingleton<IExportCommandDispatcher, ChannelExportCommandDispatcher>();
        services.AddHostedService<ExportCommandWorker>();

        return services;
    }

    /// <summary>
    /// Registers an export definition for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The source entity type.</typeparam>
    /// <typeparam name="TDefinition">The export definition implementation.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExportDefinition<TEntity, TDefinition>(
        this IServiceCollection services)
        where TEntity : class
        where TDefinition : ExportDefinition<TEntity>
    {
        services.AddSingleton<ExportDefinition<TEntity>, TDefinition>();
        services.AddSingleton<IExportDefinitionDescriptor>(sp =>
            sp.GetRequiredService<ExportDefinition<TEntity>>());
        return services;
    }

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
