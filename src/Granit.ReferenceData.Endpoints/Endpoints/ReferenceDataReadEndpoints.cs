using Granit.Querying;
using Granit.ReferenceData.Domain;
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

    private static async Task<Ok<PagedResult<TEntity>>> GetAllAsync<TEntity>(
        IReferenceDataStoreReader<TEntity> storeReader,
        [AsParameters] ReferenceDataQueryParameters parameters,
        CancellationToken cancellationToken = default)
        where TEntity : ReferenceDataEntity
    {
        ReferenceDataQuery query = new(
            ActiveOnly: parameters.ActiveOnly,
            SearchTerm: parameters.Search,
            SortBy: parameters.SortBy,
            Descending: parameters.Descending,
            Page: parameters.Page,
            PageSize: parameters.PageSize);

        PagedResult<TEntity> result = await storeReader.GetAllAsync(query, cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<TEntity>, NotFound>> GetByCodeAsync<TEntity>(
        string code,
        IReferenceDataStoreReader<TEntity> storeReader,
        CancellationToken cancellationToken = default)
        where TEntity : ReferenceDataEntity
    {
        TEntity? entity = await storeReader.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);

        if (entity is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(entity);
    }
}
