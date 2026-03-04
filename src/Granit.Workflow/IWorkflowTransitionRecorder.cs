namespace Granit.Workflow;

/// <summary>
/// Persists workflow transition records to the underlying store.
/// Decouples functional packages from the EF Core implementation.
/// </summary>
public interface IWorkflowTransitionRecorder
{
    /// <summary>
    /// Records a workflow state transition for auditing (HDS audit trail).
    /// </summary>
    /// <param name="entityType">Logical entity type name (e.g. "TemplateRevision").</param>
    /// <param name="entityId">Entity identifier.</param>
    /// <param name="previousState">State before the transition.</param>
    /// <param name="newState">State after the transition.</param>
    /// <param name="userId">User who triggered the transition.</param>
    /// <param name="comment">Optional regulatory comment or justification.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordTransitionAsync(
        string entityType,
        string entityId,
        string previousState,
        string newState,
        string userId,
        string? comment,
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}
