using Granit.DataExchange.Endpoints.Dtos.Export;
using Granit.DataExchange.Endpoints.Dtos.Import;
using Granit.DataExchange.Endpoints.Internal.Export;
using Granit.DataExchange.Endpoints.Internal.Import;
using Granit.DataExchange.Export;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.DataExchange.Endpoints.Endpoints.Export;

/// <summary>
/// Export job creation, status polling, and file download endpoints.
/// </summary>
internal static class DataExportExecutionEndpoints
{
    /// <summary>
    /// Registers POST /jobs, GET /jobs/{jobId}, GET /jobs/{jobId}/download onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapExportExecutionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/jobs", CreateExportJobAsync)
            .WithName("CreateExportJob")
            .WithSummary("Creates and dispatches an export job (sync or background).");

        group.MapGet("/jobs/{jobId:guid}", GetJobStatusAsync)
            .WithName("GetExportJobStatus")
            .WithSummary("Returns the current status of an export job.");

        group.MapGet("/jobs/{jobId:guid}/download", DownloadAsync)
            .WithName("DownloadExportFile")
            .WithSummary("Downloads the generated export file for a completed job.");

        return group;
    }

    private static async Task<Results<Created<ExportJobResponse>, BadRequest<string>>> CreateExportJobAsync(
        CreateExportJobRequest request,
        IExportOrchestrator orchestrator,
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        IExportDefinitionDescriptor? descriptor =
            ExportDefinitionResolver.FindByName(serviceProvider, request.DefinitionName);
        if (descriptor is null)
        {
            return TypedResults.BadRequest($"Unknown export definition '{request.DefinitionName}'.");
        }

        if (!descriptor.SupportedFormats.Contains(request.Format, StringComparer.OrdinalIgnoreCase))
        {
            return TypedResults.BadRequest(
                $"Format '{request.Format}' is not supported. Allowed: {string.Join(", ", descriptor.SupportedFormats)}.");
        }

        ExportRequest exportRequest = new(
            request.DefinitionName,
            request.Format,
            request.SelectedFields,
            request.IncludeIdForImport,
            request.Sort,
            request.Filter,
            request.Presets,
            request.Search);

        ExportJobResult result = await orchestrator.ExportAsync(exportRequest, ct).ConfigureAwait(false);

        ExportJob? job = await orchestrator.GetJobAsync(result.JobId, ct).ConfigureAwait(false);
        ExportJobResponse response = job is not null
            ? ExportJobResponse.FromJob(job)
            : new ExportJobResponse(result.JobId, request.DefinitionName, request.Format,
                result.Status, null, null, null, DateTimeOffset.UtcNow, null);

        return TypedResults.Created($"/jobs/{result.JobId}", response);
    }

    private static async Task<Results<Ok<ExportJobResponse>, NotFound>> GetJobStatusAsync(
        Guid jobId,
        IExportOrchestrator orchestrator,
        CancellationToken ct)
    {
        ExportJob? job = await orchestrator.GetJobAsync(jobId, ct).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ExportJobResponse.FromJob(job));
    }

    private static async Task<Results<FileStreamHttpResult, NotFound, BadRequest<string>>> DownloadAsync(
        Guid jobId,
        IExportOrchestrator orchestrator,
        CancellationToken ct)
    {
        ExportJob? job = await orchestrator.GetJobAsync(jobId, ct).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        if (job.Status != ExportJobStatus.Completed)
        {
            return TypedResults.BadRequest(
                $"Export job is not completed. Current status: '{job.Status}'.");
        }

        ExportDownload? download = await orchestrator.GetDownloadAsync(jobId, ct).ConfigureAwait(false);
        if (download is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.File(download.Content, download.MimeType, download.FileName);
    }
}
