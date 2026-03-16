using Microsoft.Extensions.AI;

namespace Granit.AI;

/// <summary>
/// Factory that resolves an <see cref="IEmbeddingGenerator{String, Embedding}"/> for a specific workspace.
/// </summary>
public interface IAIEmbeddingGeneratorFactory
{
    /// <summary>
    /// Creates an <see cref="IEmbeddingGenerator{String, Embedding}"/> for the specified workspace.
    /// </summary>
    /// <param name="workspaceName">
    /// Workspace name. If <c>null</c>, the default workspace is used.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully configured embedding generator.</returns>
    /// <exception cref="Exceptions.AIWorkspaceNotFoundException">Workspace not found.</exception>
    /// <exception cref="Exceptions.AIProviderNotRegisteredException">No provider registered for the workspace's provider name.</exception>
    Task<IEmbeddingGenerator<string, Embedding<float>>> CreateAsync(
        string? workspaceName = null,
        CancellationToken cancellationToken = default);
}
