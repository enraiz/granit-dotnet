using Granit.DataExchange.Csv.Internal.Export;
using Granit.DataExchange.Csv.Internal.Import;
using Granit.DataExchange.Export;
using Granit.DataExchange.Import.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataExchange.Csv;

/// <summary>
/// Extension methods for registering <c>Granit.DataExchange.Csv</c> services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Sep-based CSV file parser (import) and the CSV export writer.
    /// </summary>
    /// <remarks>
    /// Registers the following services:
    /// <list type="bullet">
    ///   <item><see cref="IFileParser"/> → <c>SepCsvFileParser</c> (singleton) — import.</item>
    ///   <item><see cref="IExportWriter"/> → <c>CsvExportWriter</c> (singleton) — export.</item>
    /// </list>
    /// <para>
    /// Multiple <see cref="IFileParser"/> and <see cref="IExportWriter"/> implementations
    /// can coexist (CSV + Excel). The pipeline dispatches based on MIME type (import)
    /// or format name (export).
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitDataExchangeCsv(this IServiceCollection services)
    {
        services.AddSingleton<IFileParser, SepCsvFileParser>();
        services.AddSingleton<IExportWriter, CsvExportWriter>();
        return services;
    }
}
