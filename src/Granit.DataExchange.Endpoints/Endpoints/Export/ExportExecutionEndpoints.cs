using Granit.DataExchange.Endpoints.Dtos.Export;
using Granit.DataExchange.Endpoints.Dtos.Import;
using Granit.DataExchange.Endpoints.Internal.Export;
using Granit.DataExchange.Endpoints.Internal.Import;
using Granit.DataExchange.Export;
using Granit.Timing;
using Granit.Validation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Granit.DataExchange.Endpoints.Endpoints.Export;

/// <summary>
/// Export job creation, status polling, and file download endpoints.
/// </summary>
internal static class ExportExecutionEndpoints
{
    /// <summary>
    /// Registers POST /jobs, GET /jobs/{jobId}, GET /jobs/{jobId}/download onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapExportExecutionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/jobs", CreateExportJobAsync)
            .WithName("CreateExportJob")
            .WithSummary("Creates and dispatches an export job (sync or background).")
            .ValidateBody<CreateExportJobRequest>();

        group.MapGet("/jobs/{jobId:guid}", GetJobStatusAsync)
            .WithName("GetExportJobStatus")
            .WithSummary("Returns the current status of an export job.");

        group.MapGet("/jobs/{jobId:guid}/download", DownloadAsync)
            .WithName("DownloadExportFile")
            .WithSummary("Downloads the generated export file for a completed job.");

        return group;
    }

    private static async Task<Results<Created<ExportJobResponse>, ProblemHttpResult>> CreateExportJobAsync(
        CreateExportJobRequest request,
        IExportOrchestrator orchestrator,
        IServiceProvider serviceProvider,
        [FromServices] IClock clock,
        CancellationToken cancellationToken)
    {
        IExportDefinitionDescriptor? descriptor =
            ExportDefinitionResolver.FindByName(serviceProvider, request.DefinitionName);
        if (descriptor is null)
        {
            return TypedResults.Problem(
                detail: $"Unknown export definition '{request.DefinitionName}'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!descriptor.SupportedFormats.Contains(request.Format, StringComparer.OrdinalIgnoreCase))
        {
            return TypedResults.Problem(
                detail: $"Format '{request.Format}' is not supported. Allowed: {string.Join(", ", descriptor.SupportedFormats)}.",
                statusCode: StatusCodes.Status400BadRequest);
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

        ExportJobResult result = await orchestrator.ExportAsync(exportRequest, cancellationToken).ConfigureAwait(false);

        ExportJob? job = await orchestrator.GetJobAsync(result.JobId, cancellationToken).ConfigureAwait(false);
        ExportJobResponse response = job is not null
            ? ExportJobResponse.FromJob(job)
            : new ExportJobResponse(result.JobId, request.DefinitionName, request.Format,
                result.Status, null, null, null, clock.Now, null);

        return TypedResults.Created($"/jobs/{result.JobId}", response);
    }

    private static async Task<Results<Ok<ExportJobResponse>, NotFound>> GetJobStatusAsync(
        Guid jobId,
        IExportOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        ExportJob? job = await orchestrator.GetJobAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ExportJobResponse.FromJob(job));
    }

    private static async Task<Results<FileStreamHttpResult, NotFound, ProblemHttpResult>> DownloadAsync(
        Guid jobId,
        IExportOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        ExportJob? job = await orchestrator.GetJobAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        if (job.Status != ExportJobStatus.Completed)
        {
            return TypedResults.Problem(
                detail: $"Export job is not completed. Current status: '{job.Status}'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        ExportDownload? download = await orchestrator.GetDownloadAsync(jobId, cancellationToken).ConfigureAwait(false);
        if (download is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.File(download.Content, download.MimeType, download.FileName);
    }
}
