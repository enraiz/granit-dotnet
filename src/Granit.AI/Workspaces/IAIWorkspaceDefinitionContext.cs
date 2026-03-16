namespace Granit.AI.Workspaces;

/// <summary>
/// Context passed to <see cref="IAIWorkspaceDefinitionProvider.Define"/> for registering system workspaces.
/// </summary>
public interface IAIWorkspaceDefinitionContext
{
    /// <summary>
    /// Registers a system workspace.
    /// </summary>
    /// <param name="workspace">Workspace to register. <see cref="AIWorkspace.Kind"/> is forced to <see cref="AIWorkspaceKind.System"/>.</param>
    /// <exception cref="InvalidOperationException">A workspace with the same name is already registered.</exception>
    void Add(AIWorkspace workspace);
}
