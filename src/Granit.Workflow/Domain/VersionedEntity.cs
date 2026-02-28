namespace Granit.Workflow.Domain;

/// <summary>
/// Backwards-compatible alias for <see cref="VersionedWorkflowEntity"/>.
/// </summary>
[Obsolete("Use VersionedWorkflowEntity instead. This alias will be removed in a future version.")]
public abstract class VersionedEntity : VersionedWorkflowEntity;
