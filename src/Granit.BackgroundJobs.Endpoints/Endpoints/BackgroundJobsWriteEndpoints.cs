using Granit.BackgroundJobs.Abstractions;
using Granit.Core.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.BackgroundJobs.Endpoints.Endpoints;

/// <summary>
/// POST endpoints for controlling background job execution.
/// </summary>
internal static class BackgroundJobsWriteEndpoints
{
    /// <summary>
    /// Registers POST /{name}/pause, POST /{name}/resume, POST /{name}/trigger
    /// onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapWriteEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{name}/pause", PauseJobAsync)
            .WithName("PauseBackgroundJob")
            .WithSummary("Pauses a recurring background job. The current execution completes normally.");

        group.MapPost("/{name}/resume", ResumeJobAsync)
            .WithName("ResumeBackgroundJob")
            .WithSummary("Resumes a paused background job and schedules its next occurrence.");

        group.MapPost("/{name}/trigger", TriggerJobAsync)
            .WithName("TriggerBackgroundJob")
            .WithSummary("Triggers an immediate execution of the job, independent of its schedule.");

        return group;
    }

    private static async Task<Results<NoContent, NotFound>> PauseJobAsync(
        string name,
        IBackgroundJobWriter writer,
        CancellationToken cancellationToken)
    {
        try
        {
            await writer.PauseAsync(name, cancellationToken).ConfigureAwait(false);
            return TypedResults.NoContent();
        }
        catch (EntityNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> ResumeJobAsync(
        string name,
        IBackgroundJobWriter writer,
        CancellationToken cancellationToken)
    {
        try
        {
            await writer.ResumeAsync(name, cancellationToken).ConfigureAwait(false);
            return TypedResults.NoContent();
        }
        catch (EntityNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Accepted, NotFound>> TriggerJobAsync(
        string name,
        IBackgroundJobWriter writer,
        CancellationToken cancellationToken)
    {
        try
        {
            await writer.TriggerNowAsync(name, cancellationToken).ConfigureAwait(false);
            return TypedResults.Accepted((string?)null);
        }
        catch (EntityNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }
}
