using Granit.Core.Domain;

namespace Granit.Workflow.Domain;

/// <summary>
/// Interface for entities with a versioned publication lifecycle.
/// Multiple rows can share the same <see cref="BusinessId"/>, each with a different
/// <see cref="Version"/> number and <see cref="LifecycleStatus"/>.
/// </summary>
/// <remarks>
/// <para>
/// Extends <see cref="IPublishable"/> to integrate with the global query filter
/// system (<c>WHERE IsPublished = true</c>).
/// </para>
/// <para>
/// At most one version per <see cref="BusinessId"/> may have
/// <see cref="WorkflowLifecycleStatus.Published"/> at any time.
/// This invariant is enforced by a unique filtered index in PostgreSQL.
/// </para>
/// </remarks>
public interface IVersionedEntity : IPublishable
{
    /// <summary>Stable business identifier shared across all versions of this logical entity.</summary>
    Guid BusinessId { get; set; }

    /// <summary>Monotonically increasing version number (1-based).</summary>
    int Version { get; set; }

    /// <summary>Lifecycle status of this version.</summary>
    WorkflowLifecycleStatus LifecycleStatus { get; set; }
}
