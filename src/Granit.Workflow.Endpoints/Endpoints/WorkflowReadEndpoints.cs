using Granit.Workflow.Endpoints.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Granit.Workflow.Endpoints.Endpoints;

/// <summary>
/// GET endpoints for querying workflow status and transition history.
/// </summary>
internal static class WorkflowReadEndpoints
{
    /// <summary>
    /// Registers GET endpoints onto the given route group.
    /// </summary>
    internal static RouteGroupBuilder MapReadEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{entityType}/{entityId}/history", GetTransitionHistoryAsync)
            .WithName("GetWorkflowTransitionHistory")
            .WithSummary("Returns the HDS-compliant audit trail of workflow transitions for an entity.");

        return group;
    }

    private static async Task<Ok<IReadOnlyList<TransitionHistoryResponse>>> GetTransitionHistoryAsync(
        string entityType,
        string entityId,
        IWorkflowHistoryQuery historyQuery,
        CancellationToken ct)
    {
        IReadOnlyList<TransitionHistoryResponse> history = await historyQuery.GetHistoryAsync(
            entityType, entityId, ct);
        return TypedResults.Ok(history);
    }
}
