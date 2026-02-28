using Granit.Core.Domain;

namespace Granit.Workflow.Domain;

/// <summary>
/// Base class for versioned entities with workflow lifecycle.
/// Combines <see cref="IVersionedEntity"/> with <see cref="IWorkflowStateful"/>
/// to integrate with both the global query filter system and the
/// <c>WorkflowTransitionInterceptor</c> audit trail.
/// </summary>
/// <remarks>
/// <para>
/// Inherits from <see cref="AuditedEntity"/> for CreatedAt/By and ModifiedAt/By tracking.
/// </para>
/// <para>
/// <see cref="IsPublished"/> is kept in sync with <see cref="LifecycleStatus"/> by the
/// <c>WorkflowTransitionInterceptor</c> — set to <c>true</c> only when
/// <see cref="LifecycleStatus"/> is <see cref="WorkflowLifecycleStatus.Published"/>.
/// </para>
/// <para>
/// Derived classes must provide <see cref="IWorkflowStateful.WorkflowEntityType"/>
/// by implementing the static abstract member explicitly.
/// </para>
/// </remarks>
public abstract class VersionedEntity : AuditedEntity, IVersionedEntity, IWorkflowStateful
{
    /// <inheritdoc/>
    public Guid BusinessId { get; set; }

    /// <inheritdoc/>
    public int Version { get; set; }

    /// <inheritdoc/>
    public WorkflowLifecycleStatus LifecycleStatus { get; set; }

    /// <inheritdoc/>
    public bool IsPublished { get; set; }

    /// <inheritdoc/>
    static string IWorkflowStateful.StatusPropertyName => nameof(LifecycleStatus);

    /// <summary>
    /// Logical entity type name for the audit trail. Must be overridden by derived classes.
    /// </summary>
    static string IWorkflowStateful.WorkflowEntityType =>
        throw new NotSupportedException(
            "Derived classes must implement IWorkflowStateful.WorkflowEntityType explicitly.");

    /// <inheritdoc/>
    public virtual string GetWorkflowEntityId() => Id.ToString();
}
