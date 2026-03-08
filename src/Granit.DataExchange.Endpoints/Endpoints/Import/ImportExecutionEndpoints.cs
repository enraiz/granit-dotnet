using Granit.DataExchange.Endpoints.Dtos.Export;
using Granit.DataExchange.Endpoints.Dtos.Import;
using Granit.DataExchange.Import.Domain;
using Granit.DataExchange.Import.Messages;
using Granit.DataExchange.Import.Pipeline;
using Granit.DataExchange.Import.Reporting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.DataExchange.Endpoints.Endpoints.Import;

/// <summary>
/// Execution, dry-run, status, and cancellation endpoints (Story #497).
/// </summary>
internal static class ImportExecutionEndpoints
{
    /// <summary>
    /// Registers POST /{jobId}/execute, POST /{jobId}/dry-run, GET /{jobId},
    /// DELETE /{jobId} onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapExecutionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{jobId:guid}/execute", ExecuteAsync)
            .WithName("ExecuteImportJob")
            .WithSummary("Dispatches the import job for asynchronous background execution.");

        group.MapPost("/{jobId:guid}/dry-run", DryRunAsync)
            .WithName("DryRunImportJob")
            .WithSummary("Executes a dry-run of the import (validates without persisting data).");

        group.MapGet("/{jobId:guid}", GetStatusAsync)
            .WithName("GetImportJobStatus")
            .WithSummary("Returns the current status of an import job.");

        group.MapDelete("/{jobId:guid}", CancelAsync)
            .WithName("CancelImportJob")
            .WithSummary("Cancels an import job that has not yet started execution.");

        return group;
    }

    private static async Task<Results<Accepted, NotFound, BadRequest<string>>> ExecuteAsync(
        Guid jobId,
        IImportJobReader jobReader,
        IImportCommandDispatcher dispatcher,
        CancellationToken ct)
    {
        ImportJob? job = await jobReader.GetAsync(jobId, ct).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        if (job.Status != ImportJobStatus.Mapped)
        {
            return TypedResults.BadRequest(
                $"Cannot execute import job in status '{job.Status}'. Expected: Mapped.");
        }

        ExecuteImportCommand command = new(job.Id, job.DefinitionName);
        await dispatcher.DispatchAsync(command, ct).ConfigureAwait(false);

        return TypedResults.Accepted($"/{job.Id}");
    }

    private static async Task<Results<Ok<ImportReportResponse>, NotFound, BadRequest<string>>> DryRunAsync(
        Guid jobId,
        IImportJobReader jobReader,
        IImportOrchestrator orchestrator,
        CancellationToken ct)
    {
        ImportJob? job = await jobReader.GetAsync(jobId, ct).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        if (job.Status != ImportJobStatus.Mapped)
        {
            return TypedResults.BadRequest(
                $"Cannot dry-run import job in status '{job.Status}'. Expected: Mapped.");
        }

        ImportReport report = await orchestrator.DryRunAsync(jobId, ct).ConfigureAwait(false);

        return TypedResults.Ok(ImportReportResponse.FromReport(jobId, report));
    }

    private static async Task<Results<Ok<ImportJobResponse>, NotFound>> GetStatusAsync(
        Guid jobId,
        IImportJobReader jobReader,
        CancellationToken ct)
    {
        ImportJob? job = await jobReader.GetAsync(jobId, ct).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ImportJobResponse.FromJob(job));
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> CancelAsync(
        Guid jobId,
        IImportJobReader jobReader,
        IImportJobWriter jobWriter,
        IImportFileProvider fileProvider,
        CancellationToken ct)
    {
        ImportJob? job = await jobReader.GetAsync(jobId, ct).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        if (job.Status is ImportJobStatus.Executing or ImportJobStatus.Completed
            or ImportJobStatus.PartiallyCompleted or ImportJobStatus.Failed)
        {
            return TypedResults.BadRequest(
                $"Cannot cancel import job in status '{job.Status}'.");
        }

        job.Status = ImportJobStatus.Cancelled;
        await jobWriter.UpdateAsync(job, ct).ConfigureAwait(false);
        await fileProvider.DeleteAsync(job.BlobReference, ct).ConfigureAwait(false);

        return TypedResults.NoContent();
    }
}
