using Granit.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataImport.Excel;

/// <summary>
/// Registers the Sylvan-based Excel file parser for the data import pipeline.
/// </summary>
[DependsOn(typeof(GranitDataImportModule))]
public sealed class GranitDataImportExcelModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitDataImportExcel();
}
