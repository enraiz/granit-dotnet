using Granit.AI.Workspaces;

namespace Granit.AI.Internal;

/// <summary>
/// Collects system workspace definitions from all <see cref="IAIWorkspaceDefinitionProvider"/> instances.
/// </summary>
internal sealed class AIWorkspaceDefinitionContext : IAIWorkspaceDefinitionContext
{
    private readonly Dictionary<string, AIWorkspace> _workspaces = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, AIWorkspace> Workspaces => _workspaces;

    public void Add(AIWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        AIWorkspace systemWorkspace = workspace with { Kind = AIWorkspaceKind.System };

        if (!_workspaces.TryAdd(systemWorkspace.Name, systemWorkspace))
        {
            throw new InvalidOperationException(
                $"A system workspace named '{systemWorkspace.Name}' is already registered.");
        }
    }
}
