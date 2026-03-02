using Granit.DataImport.Excel.Internal;
using Granit.DataImport.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataImport.Excel;

/// <summary>
/// Extension methods for registering <c>Granit.DataImport.Excel</c> services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Sylvan-based Excel file parser.
    /// </summary>
    /// <remarks>
    /// Registers the following service:
    /// <list type="bullet">
    ///   <item><see cref="IFileParser"/> → <c>SylvanExcelFileParser</c> (singleton)</item>
    /// </list>
    /// <para>
    /// Multiple <see cref="IFileParser"/> implementations can coexist (CSV + Excel).
    /// The pipeline dispatches to the first parser whose <see cref="IFileParser.CanParse"/>
    /// returns <c>true</c> for the uploaded file's MIME type.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGranitDataImportExcel(this IServiceCollection services) =>
        services.AddSingleton<IFileParser, SylvanExcelFileParser>();
}
