using Granit.ReferenceData.Endpoints.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.ReferenceData.Endpoints.Endpoints;

/// <summary>
/// GET endpoints for querying reference data entries.
/// </summary>
internal static class ReferenceDataReadEndpoints
{
    /// <summary>
    /// Registers GET / and GET /{code} onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapReadEndpoints<TEntity>(
        this RouteGroupBuilder group)
        where TEntity : ReferenceDataEntity
    {
        group.MapGet("/", GetAllAsync<TEntity>)
            .WithName($"GetAll{typeof(TEntity).Name}")
            .WithSummary($"Returns a filtered, paginated list of {typeof(TEntity).Name} entries.");

        group.MapGet("/{code}", GetByCodeAsync<TEntity>)
            .WithName($"Get{typeof(TEntity).Name}ByCode")
            .WithSummary($"Returns a single {typeof(TEntity).Name} entry by code.");

        return group;
    }

    private static async Task<Ok<ReferenceDataListResponse<TEntity>>> GetAllAsync<TEntity>(
        IReferenceDataStore<TEntity> store,
        [AsParameters] ReferenceDataQueryParameters parameters,
        CancellationToken ct = default)
        where TEntity : ReferenceDataEntity
    {
        ReferenceDataQuery query = new(
            ActiveOnly: parameters.ActiveOnly,
            SearchTerm: parameters.Search,
            SortBy: parameters.SortBy,
            Descending: parameters.Descending,
            Skip: parameters.Skip,
            Take: parameters.Take);

        ReferenceDataResult<TEntity> result = await store.GetAllAsync(query, ct).ConfigureAwait(false);

        return TypedResults.Ok(new ReferenceDataListResponse<TEntity>(result.Items, result.TotalCount));
    }

    private static async Task<Results<Ok<TEntity>, NotFound>> GetByCodeAsync<TEntity>(
        string code,
        IReferenceDataStore<TEntity> store,
        CancellationToken ct = default)
        where TEntity : ReferenceDataEntity
    {
        TEntity? entity = await store.GetByCodeAsync(code, ct).ConfigureAwait(false);

        if (entity is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(entity);
    }
}
