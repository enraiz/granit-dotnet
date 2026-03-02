using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.BackgroundJobs.Endpoints.Endpoints;

/// <summary>
/// GET endpoints for monitoring background job status.
/// </summary>
internal static class BackgroundJobsReadEndpoints
{
    /// <summary>
    /// Registers GET / and GET /{name} onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapReadEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllJobsAsync)
            .WithName("GetAllBackgroundJobs")
            .WithSummary("Returns the current status of all registered background jobs.");

        group.MapGet("/{name}", GetJobByNameAsync)
            .WithName("GetBackgroundJobByName")
            .WithSummary("Returns the status of a specific background job.");

        return group;
    }

    private static async Task<Ok<IReadOnlyList<BackgroundJobStatus>>> GetAllJobsAsync(
        IBackgroundJobManager manager,
        CancellationToken ct) =>
        TypedResults.Ok(await manager.GetAllAsync(ct).ConfigureAwait(false));

    private static async Task<Results<Ok<BackgroundJobStatus>, NotFound>> GetJobByNameAsync(
        string name,
        IBackgroundJobManager manager,
        CancellationToken ct)
    {
        BackgroundJobStatus? job = await manager.FindAsync(name, ct).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(job);
    }
}
