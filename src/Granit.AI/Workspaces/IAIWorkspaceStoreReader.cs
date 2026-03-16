namespace Granit.AI.Workspaces;

/// <summary>
/// Persistence abstraction for reading dynamic AI workspaces.
/// </summary>
/// <remarks>
/// Implemented by <c>Granit.AI.EntityFrameworkCore</c>.
/// Default: <see cref="NullAIWorkspaceStoreReader"/> (returns empty results).
/// </remarks>
public interface IAIWorkspaceStoreReader
{
    /// <summary>
    /// Returns the dynamic workspace with the given name for the current tenant, or <c>null</c>.
    /// </summary>
    Task<AIWorkspace?> FindAsync(string workspaceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active dynamic workspaces for the current tenant.
    /// </summary>
    Task<IReadOnlyList<AIWorkspace>> GetAllAsync(CancellationToken cancellationToken = default);
}
