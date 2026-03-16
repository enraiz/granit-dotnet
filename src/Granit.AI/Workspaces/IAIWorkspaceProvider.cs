namespace Granit.AI.Workspaces;

/// <summary>
/// Read-only access to AI workspace configurations.
/// </summary>
/// <remarks>
/// Resolves workspaces from all sources: system-defined (code) and dynamic (database).
/// System workspaces take precedence over dynamic ones with the same name.
/// Results are scoped to the current tenant when multi-tenancy is active.
/// </remarks>
public interface IAIWorkspaceProvider
{
    /// <summary>
    /// Returns the workspace with the given name, or <c>null</c> if not found.
    /// </summary>
    /// <param name="workspaceName">Workspace name to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AIWorkspace?> GetAsync(string workspaceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active workspaces visible to the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<AIWorkspace>> GetAllAsync(CancellationToken cancellationToken = default);
}
