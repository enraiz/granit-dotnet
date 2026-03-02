using Granit.Core.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataImport.Csv;

/// <summary>
/// Registers the Sep-based CSV file parser for the data import pipeline.
/// </summary>
[DependsOn(typeof(GranitDataImportModule))]
public sealed class GranitDataImportCsvModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitDataImportCsv();
}
