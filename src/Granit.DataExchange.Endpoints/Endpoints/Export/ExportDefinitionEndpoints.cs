using Granit.DataExchange.Endpoints.Dtos.Export;
using Granit.DataExchange.Endpoints.Dtos.Import;
using Granit.DataExchange.Endpoints.Internal.Export;
using Granit.DataExchange.Endpoints.Internal.Import;
using Granit.DataExchange.Export;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Granit.DataExchange.Endpoints.Endpoints.Export;

/// <summary>
/// Export definition listing and field introspection endpoints.
/// </summary>
internal static class ExportDefinitionEndpoints
{
    /// <summary>
    /// Registers GET /definitions, GET /definitions/{name}/fields onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapExportDefinitionEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/definitions", ListDefinitionsAsync)
            .WithName("ListExportDefinitions")
            .WithSummary("Lists all registered export definitions.");

        group.MapGet("/definitions/{name}/fields", GetFieldsAsync)
            .WithName("GetExportDefinitionFields")
            .WithSummary("Returns the available fields for a given export definition.");

        return group;
    }

    private static Ok<IReadOnlyList<ExportDefinitionResponse>> ListDefinitionsAsync(
        IServiceProvider serviceProvider)
    {
        IEnumerable<IExportDefinitionDescriptor> descriptors =
            serviceProvider.GetServices<IExportDefinitionDescriptor>();

        IReadOnlyList<ExportDefinitionResponse> response = descriptors
            .Select(ExportDefinitionResponse.FromDescriptor)
            .ToList()
            .AsReadOnly();

        return TypedResults.Ok(response);
    }

    private static Results<Ok<IReadOnlyList<ExportFieldResponse>>, NotFound> GetFieldsAsync(
        string name,
        IServiceProvider serviceProvider)
    {
        IExportDefinitionDescriptor? descriptor =
            ExportDefinitionResolver.FindByName(serviceProvider, name);
        if (descriptor is null)
        {
            return TypedResults.NotFound();
        }

        IReadOnlyList<ExportFieldResponse> fields = descriptor.GetFields()
            .Select(ExportFieldResponse.FromDescriptor)
            .ToList()
            .AsReadOnly();

        return TypedResults.Ok(fields);
    }
}
