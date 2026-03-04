using Granit.Workflow.Dtos;

namespace Granit.Workflow;

/// <summary>
/// Query service for retrieving workflow transition history from the database.
/// Implemented by the host application using its DbContext.
/// </summary>
/// <remarks>
/// This interface decouples the endpoints from any specific DbContext implementation.
/// The host application provides an implementation that queries
/// <c>WorkflowTransitionRecord</c> entities.
/// </remarks>
public interface IWorkflowHistoryQuery
{
    /// <summary>
    /// Returns the transition history for a specific entity, ordered chronologically.
    /// </summary>
    /// <param name="entityType">Logical entity type name.</param>
    /// <param name="entityId">Entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<TransitionHistoryResponse>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);
}
