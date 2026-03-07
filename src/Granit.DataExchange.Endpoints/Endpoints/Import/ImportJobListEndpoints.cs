using Granit.DataExchange.Endpoints.Dtos.Import;
using Granit.DataExchange.Import.Domain;
using Granit.DataExchange.Import.Pipeline;
using Granit.Querying;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Granit.DataExchange.Endpoints.Endpoints.Import;

/// <summary>
/// Import job listing endpoint for admin views.
/// </summary>
internal static class ImportJobListEndpoints
{
    /// <summary>
    /// Registers GET /jobs onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapImportJobListEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/jobs", ListAsync)
            .WithName("ListImportJobs")
            .WithSummary("Lists import jobs with optional status filter and pagination.");

        return group;
    }

    private static async Task<Ok<PagedResult<ImportJobResponse>>> ListAsync(
        [FromServices] IImportJobStore jobStore,
        [FromQuery] ImportJobStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        int clampedPageSize = Math.Clamp(pageSize, 1, 100);
        int clampedPage = Math.Max(page, 1);

        PagedResult<ImportJob> result = await jobStore
            .ListAsync(status, clampedPage, clampedPageSize, ct)
            .ConfigureAwait(false);

        PagedResult<ImportJobResponse> response = new(
            result.Items.Select(ImportJobResponse.FromJob).ToList(),
            result.TotalCount);

        return TypedResults.Ok(response);
    }
}
