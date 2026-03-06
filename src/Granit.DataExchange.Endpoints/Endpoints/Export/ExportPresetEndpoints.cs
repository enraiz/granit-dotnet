using Granit.DataExchange.Endpoints.Dtos.Export;
using Granit.DataExchange.Endpoints.Dtos.Import;
using Granit.DataExchange.Endpoints.Internal.Export;
using Granit.DataExchange.Endpoints.Internal.Import;
using Granit.DataExchange.Export;
using Granit.Validation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.DataExchange.Endpoints.Endpoints.Export;

/// <summary>
/// Export preset CRUD endpoints (Odoo export template pattern).
/// </summary>
internal static class ExportPresetEndpoints
{
    /// <summary>
    /// Registers GET /presets/{definitionName}, POST /presets, DELETE /presets/{definitionName}/{presetName}
    /// onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapExportPresetEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/presets/{definitionName}", ListPresetsAsync)
            .WithName("ListExportPresets")
            .WithSummary("Lists saved export presets for a given definition.");

        group.MapPost("/presets", SavePresetAsync)
            .WithName("SaveExportPreset")
            .WithSummary("Saves or updates an export preset.")
            .ValidateBody<SaveExportPresetRequest>();

        group.MapDelete("/presets/{definitionName}/{presetName}", DeletePresetAsync)
            .WithName("DeleteExportPreset")
            .WithSummary("Deletes a saved export preset.");

        return group;
    }

    private static async Task<Ok<IReadOnlyList<ExportPresetResponse>>> ListPresetsAsync(
        string definitionName,
        IExportPresetStore presetStore,
        CancellationToken ct)
    {
        IReadOnlyList<ExportPreset> presets =
            await presetStore.ListAsync(definitionName, ct).ConfigureAwait(false);

        IReadOnlyList<ExportPresetResponse> response = presets
            .Select(ExportPresetResponse.FromPreset)
            .ToList()
            .AsReadOnly();

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Created, BadRequest<string>>> SavePresetAsync(
        SaveExportPresetRequest request,
        IExportPresetStore presetStore,
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        IExportDefinitionDescriptor? descriptor =
            ExportDefinitionResolver.FindByName(serviceProvider, request.DefinitionName);
        if (descriptor is null)
        {
            return TypedResults.BadRequest($"Unknown export definition '{request.DefinitionName}'.");
        }

        if (string.IsNullOrWhiteSpace(request.PresetName))
        {
            return TypedResults.BadRequest("Preset name is required.");
        }

        if (request.SelectedFields is null || request.SelectedFields.Count == 0)
        {
            return TypedResults.BadRequest("At least one selected field is required.");
        }

        ExportPreset preset = new(
            request.DefinitionName,
            request.PresetName,
            request.SelectedFields,
            request.Format,
            request.IncludeIdForImport);

        await presetStore.SaveAsync(preset, ct).ConfigureAwait(false);

        return TypedResults.Created($"/presets/{request.DefinitionName}");
    }

    private static async Task<Results<NoContent, NotFound>> DeletePresetAsync(
        string definitionName,
        string presetName,
        IExportPresetStore presetStore,
        CancellationToken ct)
    {
        ExportPreset? existing =
            await presetStore.GetAsync(definitionName, presetName, ct).ConfigureAwait(false);
        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        await presetStore.DeleteAsync(definitionName, presetName, ct).ConfigureAwait(false);
        return TypedResults.NoContent();
    }
}
