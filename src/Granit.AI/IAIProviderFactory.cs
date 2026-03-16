using Granit.AI.Workspaces;
using Microsoft.Extensions.AI;

namespace Granit.AI;

/// <summary>
/// Extension point for AI providers to register their <see cref="IChatClient"/> factory.
/// </summary>
/// <remarks>
/// Each provider package (e.g. <c>Granit.AI.OpenAI</c>) registers an implementation
/// that handles a specific <see cref="ProviderName"/>.
/// </remarks>
public interface IAIProviderFactory
{
    /// <summary>
    /// Provider identifier this factory handles (e.g. <c>OpenAI</c>, <c>AzureOpenAI</c>, <c>Anthropic</c>, <c>Ollama</c>).
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Creates a raw <see cref="IChatClient"/> for the given workspace configuration.
    /// </summary>
    /// <remarks>
    /// The returned client is the bare provider client. The middleware pipeline
    /// (logging, usage, audit) is applied by <see cref="IAIChatClientFactory"/>.
    /// </remarks>
    /// <param name="workspace">Workspace configuration.</param>
    /// <returns>A provider-specific <c>IChatClient</c>.</returns>
    IChatClient CreateChatClient(AIWorkspace workspace);

    /// <summary>
    /// Creates a raw <see cref="IEmbeddingGenerator{String, Embedding}"/> for the given workspace, or <c>null</c> if not supported.
    /// </summary>
    /// <param name="workspace">Workspace configuration.</param>
    /// <returns>A provider-specific embedding generator, or <c>null</c>.</returns>
    IEmbeddingGenerator<string, Embedding<float>>? CreateEmbeddingGenerator(AIWorkspace workspace);
}
