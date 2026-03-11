namespace Granit.Workflow;

/// <summary>
/// Request object for recording a workflow state transition (ISO 27001 audit trail).
/// </summary>
public sealed record RecordTransitionRequest
{
    /// <summary>Logical entity type name (e.g. <c>"TemplateRevision"</c>).</summary>
    public required string EntityType { get; init; }

    /// <summary>Entity identifier.</summary>
    public required string EntityId { get; init; }

    /// <summary>State before the transition.</summary>
    public required string PreviousState { get; init; }

    /// <summary>State after the transition.</summary>
    public required string NewState { get; init; }

    /// <summary>User who triggered the transition.</summary>
    public required string UserId { get; init; }

    /// <summary>Optional regulatory comment or justification.</summary>
    public string? Comment { get; init; }

    /// <summary>Optional tenant identifier.</summary>
    public Guid? TenantId { get; init; }
}

/// <summary>
/// Persists workflow transition records to the underlying store.
/// Decouples functional packages from the EF Core implementation.
/// </summary>
public interface IWorkflowTransitionRecorder
{
    /// <summary>
    /// Records a workflow state transition for auditing (ISO 27001 audit trail).
    /// </summary>
    /// <param name="request">Transition data to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordTransitionAsync(
        RecordTransitionRequest request,
        CancellationToken cancellationToken = default);
}
