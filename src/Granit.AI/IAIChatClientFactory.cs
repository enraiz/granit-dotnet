using Microsoft.Extensions.AI;

namespace Granit.AI;

/// <summary>
/// Factory that resolves an <see cref="IChatClient"/> configured for a specific workspace.
/// </summary>
/// <remarks>
/// The returned <c>IChatClient</c> is pre-configured with the workspace's provider, model,
/// system prompt, and parameters. The middleware pipeline (logging, OpenTelemetry, usage tracking,
/// audit trail) is applied automatically via <c>ChatClientBuilder</c>.
/// </remarks>
public interface IAIChatClientFactory
{
    /// <summary>
    /// Creates an <see cref="IChatClient"/> for the specified workspace.
    /// </summary>
    /// <param name="workspaceName">
    /// Workspace name. If <c>null</c>, the default workspace from
    /// <see cref="Options.GranitAIOptions.DefaultWorkspace"/> is used.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully configured <c>IChatClient</c>.</returns>
    /// <exception cref="Exceptions.AIWorkspaceNotFoundException">Workspace not found.</exception>
    /// <exception cref="Exceptions.AIProviderNotRegisteredException">No provider registered for the workspace's provider name.</exception>
    Task<IChatClient> CreateAsync(string? workspaceName = null, CancellationToken cancellationToken = default);
}
