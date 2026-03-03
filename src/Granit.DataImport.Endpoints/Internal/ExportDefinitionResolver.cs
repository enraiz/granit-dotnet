using Granit.DataImport.Export;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataImport.Endpoints.Internal;

/// <summary>
/// Runtime resolution helper for export definitions.
/// </summary>
internal static class ExportDefinitionResolver
{
    /// <summary>
    /// Finds an <see cref="IExportDefinitionDescriptor"/> by name from the registered definitions.
    /// </summary>
    internal static IExportDefinitionDescriptor? FindByName(
        IServiceProvider serviceProvider,
        string definitionName)
    {
        IEnumerable<IExportDefinitionDescriptor> descriptors =
            serviceProvider.GetServices<IExportDefinitionDescriptor>();
        return descriptors.FirstOrDefault(d =>
            string.Equals(d.Name, definitionName, StringComparison.OrdinalIgnoreCase));
    }
}
