using Granit.BackgroundJobs.Abstractions;
using Granit.Querying;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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
            .WithSummary("Returns the current status of all registered background jobs with pagination.");

        group.MapGet("/{name}", GetJobByNameAsync)
            .WithName("GetBackgroundJobByName")
            .WithSummary("Returns the status of a specific background job.");

        return group;
    }

    private static async Task<Ok<PagedResult<BackgroundJobStatus>>> GetAllJobsAsync(
        IBackgroundJobReader reader,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = QueryingDefaults.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        int clampedPage = Math.Max(page, 1);
        int clampedPageSize = Math.Clamp(pageSize, 1, QueryingDefaults.MaxPageSize);

        IReadOnlyList<BackgroundJobStatus> all = await reader.GetAllAsync(cancellationToken).ConfigureAwait(false);

        int totalCount = all.Count;
        int skip = (clampedPage - 1) * clampedPageSize;
        IReadOnlyList<BackgroundJobStatus> items = all.Skip(skip).Take(clampedPageSize).ToList();

        return TypedResults.Ok(new PagedResult<BackgroundJobStatus>(items, totalCount));
    }

    private static async Task<Results<Ok<BackgroundJobStatus>, NotFound>> GetJobByNameAsync(
        string name,
        IBackgroundJobReader reader,
        CancellationToken cancellationToken)
    {
        BackgroundJobStatus? job = await reader.FindAsync(name, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(job);
    }
}
