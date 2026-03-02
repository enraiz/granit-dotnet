using Granit.ReferenceData.Endpoints.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.ReferenceData.Endpoints.Endpoints;

/// <summary>
/// Admin endpoints for managing reference data entries (create, update, deactivate).
/// </summary>
internal static class ReferenceDataAdminEndpoints
{
    /// <summary>
    /// Registers POST /, PUT /{code}, and DELETE /{code} onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapAdminEndpoints<TEntity>(
        this RouteGroupBuilder group,
        string? adminPolicyName)
        where TEntity : ReferenceDataEntity, new()
    {
        RouteGroupBuilder adminGroup = group.MapGroup("/");

        if (adminPolicyName is not null)
        {
            adminGroup.RequireAuthorization(adminPolicyName);
        }

        adminGroup.MapPost("/", CreateAsync<TEntity>)
            .WithName($"Create{typeof(TEntity).Name}")
            .WithSummary($"Creates a new {typeof(TEntity).Name} entry.");

        adminGroup.MapPut("/{code}", UpdateAsync<TEntity>)
            .WithName($"Update{typeof(TEntity).Name}")
            .WithSummary($"Updates an existing {typeof(TEntity).Name} entry.");

        adminGroup.MapDelete("/{code}", DeactivateAsync<TEntity>)
            .WithName($"Deactivate{typeof(TEntity).Name}")
            .WithSummary($"Deactivates a {typeof(TEntity).Name} entry (soft delete).");

        return group;
    }

    private static async Task<Created> CreateAsync<TEntity>(
        ReferenceDataCreateRequest request,
        IReferenceDataStore<TEntity> store,
        CancellationToken ct = default)
        where TEntity : ReferenceDataEntity, new()
    {
        TEntity entity = new()
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            LabelEn = request.LabelEn,
            LabelFr = request.LabelFr,
            LabelNl = request.LabelNl,
            LabelDe = request.LabelDe,
            LabelEs = request.LabelEs,
            LabelIt = request.LabelIt,
            LabelPt = request.LabelPt,
            SortOrder = request.SortOrder,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsActive = true,
        };

        await store.CreateAsync(entity, ct).ConfigureAwait(false);

        return TypedResults.Created($"{request.Code}");
    }

    private static async Task<Results<Ok, NotFound>> UpdateAsync<TEntity>(
        string code,
        ReferenceDataUpdateRequest request,
        IReferenceDataStore<TEntity> store,
        CancellationToken ct = default)
        where TEntity : ReferenceDataEntity, new()
    {
        TEntity? existing = await store.GetByCodeAsync(code, ct).ConfigureAwait(false);
        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        existing.LabelEn = request.LabelEn;
        existing.LabelFr = request.LabelFr;
        existing.LabelNl = request.LabelNl;
        existing.LabelDe = request.LabelDe;
        existing.LabelEs = request.LabelEs;
        existing.LabelIt = request.LabelIt;
        existing.LabelPt = request.LabelPt;
        existing.SortOrder = request.SortOrder;
        existing.IsActive = request.IsActive;
        existing.ValidFrom = request.ValidFrom;
        existing.ValidTo = request.ValidTo;

        await store.UpdateAsync(existing, ct).ConfigureAwait(false);

        return TypedResults.Ok();
    }

    private static async Task<Results<NoContent, NotFound>> DeactivateAsync<TEntity>(
        string code,
        IReferenceDataStore<TEntity> store,
        CancellationToken ct = default)
        where TEntity : ReferenceDataEntity, new()
    {
        TEntity? existing = await store.GetByCodeAsync(code, ct).ConfigureAwait(false);
        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        await store.SetActiveAsync(code, false, ct).ConfigureAwait(false);

        return TypedResults.NoContent();
    }
}
