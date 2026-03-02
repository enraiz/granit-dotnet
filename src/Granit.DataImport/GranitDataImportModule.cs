using Granit.Core.Modularity;
using Granit.Timing;
using Granit.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataImport;

/// <summary>
/// Granit module for the data import pipeline.
/// </summary>
/// <remarks>
/// Registers the core import infrastructure: mapping suggestion service, orchestrator,
/// and null-object defaults for optional services.
/// <para>
/// This module provides <strong>no</strong> file parser by default.
/// Add at least one parser package:
/// <list type="bullet">
///   <item><c>Granit.DataImport.Csv</c> for CSV files (Sep).</item>
///   <item><c>Granit.DataImport.Excel</c> for Excel files (Sylvan.Data.Excel).</item>
/// </list>
/// </para>
/// <para>
/// For persistence (executor, stores), add <c>Granit.DataImport.EntityFrameworkCore</c>.
/// For REST endpoints, add <c>Granit.DataImport.Endpoints</c>.
/// </para>
/// </remarks>
[DependsOn(
    typeof(GranitTimingModule),
    typeof(GranitValidationModule))]
public sealed class GranitDataImportModule : GranitModule
{
    /// <inheritdoc/>
    public override void ConfigureServices(ServiceConfigurationContext context) =>
        context.Services.AddGranitDataImport();
}
