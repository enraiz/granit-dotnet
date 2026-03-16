namespace Granit.AI.Workspaces;

/// <summary>
/// Extension point for modules to declare system workspaces in code.
/// </summary>
/// <remarks>
/// Implementations are auto-discovered from all loaded assemblies.
/// System workspaces are immutable and take precedence over dynamic workspaces.
/// </remarks>
public interface IAIWorkspaceDefinitionProvider
{
    /// <summary>
    /// Declares system workspaces.
    /// </summary>
    /// <param name="context">Context to register workspaces into.</param>
    void Define(IAIWorkspaceDefinitionContext context);
}
