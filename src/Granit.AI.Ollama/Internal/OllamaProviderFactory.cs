using Granit.AI.Ollama.Options;
using Granit.AI.Workspaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Granit.AI.Ollama.Internal;

/// <summary>
/// Ollama implementation of <see cref="IAIProviderFactory"/>.
/// Creates <see cref="OllamaApiClient"/> instances that implement both
/// <see cref="IChatClient"/> and <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/>.
/// </summary>
/// <remarks>
/// <see cref="OllamaApiClient"/> natively supports the <c>Microsoft.Extensions.AI</c>
/// abstractions. Each call creates a new client pointing at the configured
/// endpoint with the workspace model (or the default model from options).
/// </remarks>
internal sealed class OllamaProviderFactory(IOptions<OllamaOptions> options) : IAIProviderFactory
{
    /// <inheritdoc/>
    public string ProviderName => "Ollama";

    /// <inheritdoc/>
    public IChatClient CreateChatClient(AIWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        string model = workspace.Model ?? options.Value.DefaultModel;
        var endpoint = new Uri(options.Value.Endpoint);

        return new OllamaApiClient(endpoint, model);
    }

    /// <inheritdoc/>
    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(AIWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        string model = workspace.Model ?? options.Value.DefaultModel;
        var endpoint = new Uri(options.Value.Endpoint);

        return new OllamaApiClient(endpoint, model);
    }
}
