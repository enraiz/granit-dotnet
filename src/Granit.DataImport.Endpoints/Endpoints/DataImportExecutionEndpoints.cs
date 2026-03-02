using Granit.DataImport.Domain;
using Granit.DataImport.Endpoints.Dtos;
using Granit.DataImport.Messages;
using Granit.DataImport.Pipeline;
using Granit.DataImport.Reporting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.DataImport.Endpoints.Endpoints;

/// <summary>
/// Execution, dry-run, status, and cancellation endpoints (Story #497).
/// </summary>
internal static class DataImportExecutionEndpoints
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
        IImportJobStore jobStore,
        IImportCommandDispatcher dispatcher,
        CancellationToken ct)
    {
        ImportJob? job = await jobStore.GetAsync(jobId, ct);
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
        await dispatcher.DispatchAsync(command, ct);

        return TypedResults.Accepted($"/{job.Id}");
    }

    private static async Task<Results<Ok<ImportReportResponse>, NotFound, BadRequest<string>>> DryRunAsync(
        Guid jobId,
        IImportJobStore jobStore,
        IImportOrchestrator orchestrator,
        CancellationToken ct)
    {
        ImportJob? job = await jobStore.GetAsync(jobId, ct);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        if (job.Status != ImportJobStatus.Mapped)
        {
            return TypedResults.BadRequest(
                $"Cannot dry-run import job in status '{job.Status}'. Expected: Mapped.");
        }

        ImportReport report = await orchestrator.DryRunAsync(jobId, ct);

        return TypedResults.Ok(ImportReportResponse.FromReport(jobId, report));
    }

    private static async Task<Results<Ok<ImportJobResponse>, NotFound>> GetStatusAsync(
        Guid jobId,
        IImportJobStore jobStore,
        CancellationToken ct)
    {
        ImportJob? job = await jobStore.GetAsync(jobId, ct);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(ImportJobResponse.FromJob(job));
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> CancelAsync(
        Guid jobId,
        IImportJobStore jobStore,
        IImportFileProvider fileProvider,
        CancellationToken ct)
    {
        ImportJob? job = await jobStore.GetAsync(jobId, ct);
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
        await jobStore.UpdateAsync(job, ct);
        await fileProvider.DeleteAsync(job.BlobReference, ct);

        return TypedResults.NoContent();
    }
}
