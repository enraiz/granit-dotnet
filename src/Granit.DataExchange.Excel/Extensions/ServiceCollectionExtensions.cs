using Granit.DataExchange.Excel.Internal.Export;
using Granit.DataExchange.Excel.Internal.Import;
using Granit.DataExchange.Export;
using Granit.DataExchange.Import.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataExchange.Excel.Extensions;

/// <summary>
/// Extension methods for registering <c>Granit.DataExchange.Excel</c> services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Sylvan-based Excel file parser (import) and the ClosedXML-based
    /// Excel export writer.
    /// </summary>
    /// <remarks>
    /// Registers the following services:
    /// <list type="bullet">
    ///   <item><see cref="IFileParser"/> → <c>SylvanExcelFileParser</c> (singleton) — import.</item>
    ///   <item><see cref="IExportWriter"/> → <c>ClosedXmlExportWriter</c> (singleton) — export.</item>
    /// </list>
    /// <para>
    /// Multiple <see cref="IFileParser"/> and <see cref="IExportWriter"/> implementations
    /// can coexist (CSV + Excel). The pipeline dispatches based on MIME type (import)
    /// or format name (export).
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitDataExchangeExcel(this IServiceCollection services)
    {
        services.AddSingleton<IFileParser, SylvanExcelFileParser>();
        services.AddSingleton<IExportWriter, ClosedXmlExportWriter>();
        return services;
    }
}
