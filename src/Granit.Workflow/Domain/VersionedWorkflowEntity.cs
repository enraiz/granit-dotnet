using Granit.Core.Domain;

namespace Granit.Workflow.Domain;

/// <summary>
/// Base class for entities combining versioned history with a workflow lifecycle.
/// This is the "Case 3" entity: versioning + workflow for document-like entities.
/// </summary>
/// <remarks>
/// <para>
/// Inherits from <see cref="AuditedEntity"/> for CreatedAt/By and ModifiedAt/By tracking.
/// Implements both <see cref="IVersionedEntity"/> and <see cref="IWorkflowStateful"/>
/// to integrate with the global query filter system and the
/// <c>WorkflowTransitionInterceptor</c> audit trail.
/// </para>
/// <para>
/// <see cref="IsPublished"/> is kept in sync with <see cref="LifecycleStatus"/> by the
/// <c>WorkflowTransitionInterceptor</c> — set to <c>true</c> only when
/// <see cref="LifecycleStatus"/> is <see cref="WorkflowLifecycleStatus.Published"/>.
/// </para>
/// <para>
/// <see cref="IVersioned.Version"/> is auto-incremented by <c>VersioningInterceptor</c>
/// on insert.
/// </para>
/// <para>
/// For pure versioning without workflow, implement <see cref="IVersioned"/> directly.
/// For pure workflow without versioning, implement <see cref="IWorkflowStateful"/> directly.
/// </para>
/// <para>
/// Derived classes must provide <see cref="IWorkflowStateful.WorkflowEntityType"/>
/// by implementing the static abstract member explicitly.
/// </para>
/// </remarks>
public abstract class VersionedWorkflowEntity : AuditedEntity, IVersionedEntity, IWorkflowStateful
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
