namespace Granit.AI.Exceptions;

/// <summary>
/// Thrown when a requested AI workspace does not exist.
/// </summary>
public sealed class AIWorkspaceNotFoundException(string workspaceName)
    : InvalidOperationException($"AI workspace '{workspaceName}' was not found.")
{
    /// <summary>
    /// Name of the workspace that was not found.
    /// </summary>
    public string WorkspaceName { get; } = workspaceName;
}
