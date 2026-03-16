namespace Granit.AI.Workspaces;

/// <summary>
/// Write operations for dynamic AI workspaces.
/// </summary>
/// <remarks>
/// Only <see cref="AIWorkspaceKind.Dynamic"/> workspaces can be created, updated, or deleted.
/// Attempting to modify a <see cref="AIWorkspaceKind.System"/> workspace throws
/// <see cref="InvalidOperationException"/>.
/// </remarks>
public interface IAIWorkspaceManager
{
    /// <summary>
    /// Creates a new dynamic workspace.
    /// </summary>
    /// <param name="workspace">Workspace configuration. <see cref="AIWorkspace.Kind"/> must be <see cref="AIWorkspaceKind.Dynamic"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">A workspace with the same name already exists for this tenant.</exception>
    Task CreateAsync(AIWorkspace workspace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing dynamic workspace.
    /// </summary>
    /// <param name="workspace">Updated workspace configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Workspace is system-defined or not found.</exception>
    Task UpdateAsync(AIWorkspace workspace, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a dynamic workspace by name.
    /// </summary>
    /// <param name="workspaceName">Workspace name to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Workspace is system-defined.</exception>
    Task DeleteAsync(string workspaceName, CancellationToken cancellationToken = default);
}
