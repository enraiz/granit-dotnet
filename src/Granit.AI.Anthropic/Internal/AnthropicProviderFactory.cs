using Anthropic;
using Granit.AI.Anthropic.Options;
using Granit.AI.Workspaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Granit.AI.Anthropic.Internal;

/// <summary>
/// Anthropic implementation of <see cref="IAIProviderFactory"/>.
/// </summary>
/// <remarks>
/// Creates <see cref="IChatClient"/> instances backed by the Anthropic SDK.
/// Embedding generation is not supported by Anthropic and always returns <c>null</c>.
/// </remarks>
internal sealed class AnthropicProviderFactory(IOptions<AnthropicProviderOptions> options) : IAIProviderFactory
{
    /// <inheritdoc />
    public string ProviderName => "Anthropic";

    /// <inheritdoc />
    public IChatClient CreateChatClient(AIWorkspace workspace)
    {
        AnthropicProviderOptions opts = options.Value;
        AnthropicClient client = new() { ApiKey = opts.ApiKey };
        string model = workspace.Model ?? opts.DefaultModel;

        return client.AsIChatClient(model);
    }

    /// <inheritdoc />
    /// <returns>Always <c>null</c>. Anthropic does not support embedding generation.</returns>
    public IEmbeddingGenerator<string, Embedding<float>>? CreateEmbeddingGenerator(AIWorkspace workspace) =>
        null;
}
