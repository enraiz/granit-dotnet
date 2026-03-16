namespace Granit.AI.Workspaces;

/// <summary>
/// Distinguishes system-defined workspaces (immutable, declared in code)
/// from dynamic workspaces (managed via API, persisted in database).
/// </summary>
public enum AIWorkspaceKind
{
    /// <summary>
    /// Workspace declared in code via <see cref="IAIWorkspaceDefinitionProvider"/>.
    /// Cannot be modified or deleted at runtime.
    /// </summary>
    System,

    /// <summary>
    /// Workspace created and managed via the API. Persisted in the database
    /// by <c>Granit.AI.EntityFrameworkCore</c>.
    /// </summary>
    Dynamic,
}
