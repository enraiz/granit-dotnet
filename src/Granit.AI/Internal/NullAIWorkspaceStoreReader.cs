using Granit.AI.Workspaces;

namespace Granit.AI.Internal;

/// <summary>
/// No-op workspace store reader used when no persistence adapter is registered.
/// Returns empty results — only system workspaces are available.
/// </summary>
internal sealed class NullAIWorkspaceStoreReader : IAIWorkspaceStoreReader
{
    public Task<AIWorkspace?> FindAsync(string workspaceName, CancellationToken cancellationToken = default) =>
        Task.FromResult<AIWorkspace?>(null);

    public Task<IReadOnlyList<AIWorkspace>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AIWorkspace>>([]);
}
